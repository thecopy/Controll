using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting;
using Controll.Hosting.NHibernate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using Newtonsoft.Json;

namespace Controll.IntegrationTests
{
    // Since Microsoft.Owin.Host.HttpListener is not explicitly used in any test code 
    // we must use this attribute to force MSTest.exe to copy it
    [DeploymentItem("Microsoft.Owin.Host.HttpListener.dll")]
    [TestClass]
    public class HubCommunicationsTests
    {
        private const string LocalHostUrl = "http://erik-ws:10244"; // Change this to your preffered hostname (or localhost but machine name works with Fiddler)
        [TestMethod]
        public void ShouldBeAbleToLoginAsClient()
        {
            var server = new ControllStandAloneServer("http://*:10244/");

            Bootstrapper.Kernel.Rebind<ISession>()
                .ToMethod(_ => NHibernateHelper.GetSessionFactoryForMockedData().OpenSession())
                .InThreadScope();

            using(server.Start()) // Start listening on /localhost:10244/
            {
                var client = new ControllClient(LocalHostUrl);
                client.Connect();

                var logonResult = client.LogOn("username", "password");

                Assert.IsTrue(logonResult, "Client could not logon");
            }
        }

        [TestMethod]
        public void ShouldBeAbleToLoginAsZombie()
        {
            var server = new ControllStandAloneServer("http://*:10244/");

            Bootstrapper.Kernel.Rebind<ISession>()
                .ToMethod(_ => NHibernateHelper.GetSessionFactoryForMockedData().OpenSession())
                .InThreadScope();

            using (server.Start()) // Start listening on /localhost:10244/
            {
                var client = new ControllZombieClient(LocalHostUrl);

                var logonResult = client.LogOn("username", "password", "zombieName");

                Assert.IsTrue(logonResult, "Zombie could not logon");
            }
        }

        [TestMethod]
        public void ShouldBeAbleToPing()
        {
            var server = new ControllStandAloneServer("http://*:10244/");

            Bootstrapper.Kernel.Rebind<ISession>()
                .ToMethod(_ => NHibernateHelper.GetSessionFactoryForMockedData().OpenSession())
                .InThreadScope();

            using (server.Start()) // Start listening on localhost:10244/
            {
                var zombie = new ControllZombieClient(LocalHostUrl);
                var client = new ControllClient(LocalHostUrl);
                client.Connect();

                zombie.LogOn("username", "password", "zombieName");
                client.LogOn("username", "password");

                var pingEvent = new ManualResetEvent(false);
                var pongEvent = new ManualResetEvent(false);

                var pingTicket = Guid.Empty;
                var pongTicket = Guid.Empty;

                zombie.Pinged += (sender, args) =>
                    {
                        pingEvent.Set();
                        pingTicket = args.Ticket;
                    };
                client.MessageDelivered += (sender, args) =>
                    {
                        pongEvent.Set();
                        pongTicket = args.DeliveredTicket;
                    };

                Guid messageTicket = client.Ping("zombieName");

                Assert.IsTrue(pingEvent.WaitOne(1000), "Zombie did not recieve ping");
                Assert.IsTrue(pongEvent.WaitOne(1000), "Client did not recieve pong");
                
                Assert.AreEqual(messageTicket, pingTicket);
                Assert.AreEqual(messageTicket, pongTicket);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToActivateActivity()
        {
            var server = new ControllStandAloneServer("http://*:10244/");

            Bootstrapper.Kernel.Rebind<ISession>()
                .ToMethod(_ => NHibernateHelper.GetSessionFactoryForMockedData().OpenSession())
                .InThreadScope();

            using (server.Start()) // Start listening on /localhost:10244/
            {
                var zombie = new ControllZombieClient(LocalHostUrl);
                var client = new ControllClient(LocalHostUrl);
                client.Connect();

                zombie.LogOn("username", "password", "zombieName");
                client.LogOn("username", "password");

                var activatedEvent = new ManualResetEvent(false);

                var activityKey = Guid.Empty;
                var activityTicket = Guid.Empty;
                IDictionary<string, string> activityParamters = null;

                #region Send Activity Invocation
                zombie.ActivateZombie += (sender, args) =>
                {
                    activityKey = args.ActivityKey;
                    activityTicket = args.ActivityTicket;
                    activityParamters = args.Parameter;

                    activatedEvent.Set();
                };

                Guid sentActivityKey = Guid.Parse("f82a4dee-3839-4efd-8eca-0e09b2a498d3");
                var sentParameters = new Dictionary<string, string> {{"param1", "value1"}};
                var mockedActivity = new ActivityViewModel
                    {
                        CreatorName = "name",
                        Description = "mocked",
                        Key = sentActivityKey,
                        LastUpdated = DateTime.Now,
                        Name = "Mocked Activity",
                        Version = new Version(1, 2, 3, 4),
                        Commands = new List<ActivityCommandViewModel>()
                    };
                zombie.Synchronize(new List<ActivityViewModel>()
                    {
                        mockedActivity
                    }).Wait(); // Important to wait on this

                Console.WriteLine("Starting activity " + sentActivityKey);
                Guid ticket = client.StartActivity("zombieName",
                                                   sentActivityKey,
                                                   sentParameters);

                Assert.AreNotEqual(Guid.Empty, ticket, "Returned activity invocation ticked was empty");

                Assert.IsTrue(activatedEvent.WaitOne(6000), "Zombie did not recieve activity invocation order");

                Assert.AreEqual(ticket, activityTicket);
                Assert.AreEqual(sentActivityKey, activityKey);
                CollectionAssert.AreEqual(sentParameters, (Dictionary<string,string>) activityParamters);
                #endregion

                #region Start And Finish activity

                var messageType = ActivityMessageType.Failed; // Make compiler stop whining about un-initialized variable
                var activityMessageEventTicket = Guid.Empty;
                var activityMessage = "";
                var activityMessageEvent = new ManualResetEvent(false);

                client.ActivityMessageRecieved += (sender, args) =>
                    {
                        messageType = args.Type;
                        activityMessageEventTicket = args.Ticket;
                        activityMessage = args.Message;

                        activityMessageEvent.Set();
                    };

                object recievedObject = null;
                Guid activityResultTicket = Guid.Empty;
                client.ActivityResultRecieved += (sender, args) =>
                    {
                        recievedObject = args.Result;
                        activityResultTicket = args.Ticket;
                    };

                zombie.ActivityStarted(activityTicket);

                Assert.IsTrue(activityMessageEvent.WaitOne(6000), "Client did not recieve activity started message");
                Assert.AreEqual(ActivityMessageType.Started, messageType);
                Assert.AreEqual(activityTicket, activityMessageEventTicket);

                activityMessageEvent.Reset();
                zombie.ActivityResult(activityTicket, mockedActivity);
                zombie.ActivityCompleted(activityTicket, "result");

                Assert.IsTrue(activityMessageEvent.WaitOne(6000), "Client did not recieve activity finished message");
                Assert.AreEqual(ActivityMessageType.Completed, messageType);
                Assert.AreEqual(activityTicket, activityMessageEventTicket);
                Assert.AreEqual("result", activityMessage);
                
                Assert.IsNotNull(recievedObject);
                Assert.AreEqual(ticket, activityResultTicket);

                var converted = JsonConvert.DeserializeObject<ActivityViewModel>(recievedObject.ToString());
                Assert.AreEqual(mockedActivity.CreatorName, converted.CreatorName);
                Assert.AreEqual(mockedActivity.Name, converted.Name);
                Assert.AreEqual(mockedActivity.Description, converted.Description);
                Assert.AreEqual(mockedActivity.LastUpdated, converted.LastUpdated);
                Assert.AreEqual(mockedActivity.Version, converted.Version);

                #endregion
            }
        }
    }
}

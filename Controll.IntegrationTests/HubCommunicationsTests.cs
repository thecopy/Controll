using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Controll.Client;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Zombie;
using NUnit.Framework;
using Newtonsoft.Json;
using ControllClient = Controll.Client.ControllClient;

namespace Controll.IntegrationTests
{
    public class HubCommunicationsTests : StandAloneFixtureBase
    {
        // Change this to your hostname (or localhost but machine-name works with Fiddler)
        private const string LocalHostUrl = "http://erik-ws:10244";
        
        [Test]
        public void ShouldBeAbleToLoginAsClient()
        {
            var client = new ControllClient(LocalHostUrl);
            
            client.Connect("username", "password").Wait();

            client.Stop();
        }

        [Test]
        public void ShouldBeAbleToLoginAsZombie()
        {
            var client = new ZombieClient(LocalHostUrl);
            client.Connect("username", "password", "zombieName").Wait();
            client.HubConnection.Stop();
        }

        [Test]
        public void ShouldBeAbleToPing()
        {
            var client = new ControllClient(LocalHostUrl);
            var zombie = new ZombieClient(LocalHostUrl);

            client.Connect("username", "password").Wait();
            zombie.Connect("username", "password", "zombieName").Wait();

            var pingEvent = new ManualResetEvent(false);
            var pongEvent = new ManualResetEvent(false);

            var pingTicket = Guid.Empty;
            var pongTicket = Guid.Empty;

            zombie.Pinged += (ticket) =>
                {
                    pingTicket = ticket;
                    pingEvent.Set();

                    zombie.ConfirmMessageDelivery(ticket);
                };

            client.MessageDelivered += (sender, args) =>
                {
                    pongTicket = args.DeliveredTicket;
                    pongEvent.Set();
                    Console.WriteLine("Client Recieved Ping!");
                };

            Guid messageTicket = client.Ping("zombieName").Result;

            Assert.True(pingEvent.WaitOne(4000), "Zombie did not recieve ping");
            Assert.True(pongEvent.WaitOne(4000), "Client did not recieve pong");

            Assert.AreEqual(messageTicket, pingTicket);
            Assert.AreEqual(messageTicket, pongTicket);

            client.Stop();
        }

        // This is a monster test ("smoke test"). It tests: Logging in for both zombie and client,
        // sending and recieving activity messages, invocations, results and logging.
        [Test]
        public void ShouldBeAbleToActivateActivity()
        {
            var client = new ControllClient(LocalHostUrl);
            var zombie = new ZombieClient(LocalHostUrl);

            zombie.Connect("username", "password", "zombieName").Wait();
            client.Connect("username", "password").Wait();

            var activatedEvent = new ManualResetEvent(false);

            var activityKey = Guid.Empty;
            var activityTicket = Guid.Empty;
            var activityCommandName = "";
            IDictionary<string, string> activityParamters = null;

            #region Send Activity Invocation
            
            zombie.InvocationRequest += (invocationInfo) =>
                {
                    activityKey = invocationInfo.ActivityKey;
                    activityTicket = invocationInfo.Ticket;
                    activityParamters = invocationInfo.Parameter;
                    activityCommandName = invocationInfo.CommandName;

                    activatedEvent.Set();
                };

            Guid sentActivityKey = Guid.Parse("f82a4dee-3839-4efd-8eca-0e09b2a498d3");
            var sentParameters = new Dictionary<string, string> {{"param1", "value1"}};
            const string sentCommandName = "commandName";
            var mockedActivity = new ActivityViewModel
                {
                    CreatorName = "name",
                    Description = "mocked",
                    Key = sentActivityKey,
                    LastUpdated = DateTime.Now,
                    Name = "Mocked Activity",
                    Version = new Version(1, 2, 3, 4),
                    Commands = new List<ActivityCommandViewModel>
                        {
                            new ActivityCommandViewModel
                                {
                                    Label = "command-label",
                                    Name = "commandName",
                                    ParameterDescriptors = new List<ParameterDescriptorViewModel>
                                        {
                                            new ParameterDescriptorViewModel
                                                {
                                                    Description = "pd-description",
                                                    IsBoolean = true,
                                                    Label = "pd-label",
                                                    Name = "pd-name",
                                                    PickerValues = new List<PickerValueViewModel>
                                                        {
                                                            new PickerValueViewModel
                                                                {
                                                                    CommandName = "pv-commandname",
                                                                    Description = "pv-description",
                                                                    Identifier = "pv-id",
                                                                    IsCommand = true,
                                                                    Label = "pv-label",
                                                                    Parameters = new Dictionary<string, string>
                                                                        {
                                                                            {"param1", "value1"}
                                                                        }
                                                                }
                                                        }
                                                }
                                        }
                                }
                        }
                };
            zombie.Synchronize(new List<ActivityViewModel>()
                {
                    mockedActivity
                }).Wait(); // Important to wait on this

            Console.WriteLine("Starting activity " + sentActivityKey);
            Guid ticket = client.StartActivity("zombieName",
                                               sentActivityKey,
                                               sentParameters,
                                               sentCommandName).Result;

            Assert.AreNotEqual(Guid.Empty, ticket);
            Assert.True(activatedEvent.WaitOne(6000), "Zombie did not recieve activity invocation order");

            Assert.AreEqual(ticket, activityTicket);
            Assert.AreEqual(sentActivityKey, activityKey);
            Assert.AreEqual(sentCommandName, activityCommandName);
            Assert.AreEqual(sentParameters, (Dictionary<string, string>) activityParamters);

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
            var activityResultRecieved = new ManualResetEvent(false);
            client.ActivityResultRecieved += (sender, args) =>
                {
                    recievedObject = args.Result;
                    activityResultTicket = args.Ticket;
                    activityResultRecieved.Set();
                };

            zombie.ActivityMessage(activityTicket, ActivityMessageType.Started, "start!");

            Assert.True(activityMessageEvent.WaitOne(6000), "Client did not recieve activity started message");
            Assert.AreEqual(ActivityMessageType.Started, messageType);
            Assert.AreEqual(activityTicket, activityMessageEventTicket);

            activityMessageEvent.Reset();
            zombie.ActivityResult(activityTicket, mockedActivity.Commands.First());
            zombie.ActivityMessage(activityTicket, ActivityMessageType.Completed, "result");

            Assert.True(activityMessageEvent.WaitOne(6000), "Client did not recieve activity finished message");
            Assert.AreEqual(ActivityMessageType.Completed, messageType);
            Assert.AreEqual(activityTicket, activityMessageEventTicket);
            Assert.AreEqual("result", activityMessage);

            Assert.True(activityResultRecieved.WaitOne(6000), "Client did not recieve activity finished message");
            Assert.NotNull(recievedObject);
            Assert.AreEqual(ticket, activityResultTicket);

            var converted = JsonConvert.DeserializeObject<ActivityCommandViewModel>(recievedObject.ToString());
            var convertedPd = converted.ParameterDescriptors.First();
            var convertedPv = convertedPd.PickerValues.First();
            var convertedParam = convertedPv.Parameters.ElementAt(0);

            var mockedCommand = mockedActivity.Commands.First();
            var mockedPd = mockedCommand.ParameterDescriptors.First();
            var mockedPv = mockedPd.PickerValues.First();
            var mockedParam = mockedPv.Parameters.ElementAt(0);

            Assert.AreEqual(mockedCommand.Name, converted.Name);
            Assert.AreEqual(mockedCommand.Label, converted.Label);
            Assert.AreEqual(converted.ParameterDescriptors.Count(), 1);

            Assert.AreEqual(mockedPd.Name, convertedPd.Name);
            Assert.AreEqual(mockedPd.Description, convertedPd.Description);
            Assert.AreEqual(mockedPd.Label, convertedPd.Label);
            Assert.AreEqual(mockedPd.IsBoolean, convertedPd.IsBoolean);
            Assert.AreEqual(mockedPd.PickerValues.Count(), convertedPd.PickerValues.Count());

            Assert.AreEqual(mockedPv.CommandName, convertedPv.CommandName);
            Assert.AreEqual(mockedPv.Identifier, convertedPv.Identifier);
            Assert.AreEqual(mockedPv.Description, convertedPv.Description);
            Assert.AreEqual(mockedPv.Label, convertedPv.Label);
            Assert.AreEqual(mockedPv.IsCommand, convertedPv.IsCommand);
            Assert.AreEqual(mockedPv.Parameters.Count, convertedPv.Parameters.Count);

            Assert.AreEqual(mockedParam.Key, convertedParam.Key);
            Assert.AreEqual(mockedParam.Value, convertedParam.Value);

            #endregion

            #region Logging

            var logging = client.GetLogBooks(10, 0).Result.ToList();

            Assert.AreEqual(1, logging.Count);
            var book = logging.ElementAt(0);

            Assert.AreEqual("Mocked Activity", book.ActivityName);
            Assert.AreEqual(ticket, book.InvocationTicket);
            Assert.AreEqual("commandName", book.CommandLabel);
            Assert.AreEqual(sentParameters, book.Parameters);

            Assert.AreEqual(2, book.Messages.Count());
            var message1 = book.Messages.ElementAt(0);
            var message2 = book.Messages.ElementAt(1);

            Assert.AreEqual("start!", message1.Message);
            Assert.AreEqual(ActivityMessageType.Started, message1.MessageType);

            Assert.AreEqual("result", message2.Message);
            Assert.AreEqual(ActivityMessageType.Completed, message2.MessageType);
            #endregion

            client.Stop();
        }
    }
}

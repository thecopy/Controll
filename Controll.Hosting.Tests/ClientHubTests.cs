using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Hubs;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using FizzWare.NBuilder;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NHibernate;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class ClientHubTests
    {
        [TestMethod]
        public void ShouldBeAbleToAddClientWhenLoggingInAndRemoveClientFromUserWhenDisconnecting()
        {
            var mockedRepository = new Mock<IControllRepository>();
            var user = new ControllUser() { UserName = "Erik", Password = "password", Id = 1 };
            
            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));

            var hub = GetTestableClientHub(connectionId, clientState, user, mockedRepository.Object, principal: mockedPrinicipal);

            hub.SignIn();
            
            Assert.AreEqual(1, user.ConnectedClients.Count);
            Assert.AreEqual(connectionId, user.ConnectedClients[0].ConnectionId);

            mockedRepository.Setup(x => x.GetClientByConnectionId(It.Is<String>(s => s == connectionId))).Returns(user.ConnectedClients[0].ClientCommunicator);

            hub.OnDisconnected();

            Assert.AreEqual(0, user.ConnectedClients.Count);
        }
        
        [TestMethod]
        public void ShouldBeAbleToPingZombie()
        {
            var user = new ControllUser
            {
                Id = 1,
                UserName = "Erik",
                Password = "password",
                Email = "mail",
                Zombies = new List<Zombie>
                    {
                        new Zombie
                            {
                                Name = "zombie"
                            }
                    }
            };


            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";
            
            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, null, principal: mockedPrinicipal);
            hub.Clients.Caller.UserName = "Erik";

            hub.MockedMessageQueueService
                .Setup(x => x.InsertPingMessage(It.Is<Zombie>(z => z == user.Zombies[0]), It.Is<String>(s => s == hub.Context.ConnectionId)))
                .Returns(new PingQueueItem { Ticket = Guid.NewGuid() }).Verifiable("InsertPingMessage was not called by hub");

            var ticket = hub.PingZombie("zombie");

            Assert.AreNotEqual(Guid.Empty, ticket, "Ping Ticket was emtpy");
            hub.MockedMessageQueueService.Verify(x => x.InsertPingMessage(It.Is<Zombie>(z => z == user.Zombies[0]), It.Is<String>(s => s == hub.Context.ConnectionId)), Times.Once());
        }
        
        [TestMethod]
        public void ShouldBeAbleToGetZombieOnlineStatus()
        {
            var user = new ControllUser
                {
                    Id = 1,
                    UserName = "Erik",
                    Password = "password",
                    Email = "mail",
                    Zombies = new List<Zombie>
                    {
                        new Zombie
                            {
                                Name = "zombie"
                            }
                    }
                };
            
            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, null, principal: mockedPrinicipal);
            hub.Clients.Caller.UserName = "Erik";


             user.Zombies[0].ConnectedClients.Add(new ControllClient{ ConnectionId = "conn"});

            var result = hub.IsZombieOnline("zombie");
            Assert.IsTrue(result);

            user.Zombies[0].ConnectedClients[0].ConnectionId = null;
            result = hub.IsZombieOnline("zombie");

            Assert.IsFalse(result);

            user.Zombies[0].ConnectedClients.Clear();
            result = hub.IsZombieOnline("zombie");

            Assert.IsFalse(result);
        }


        [TestMethod]
        public void ShouldThrowWhenGettingZombieOnlineStatusOnNonExistingZombie()
        {
            var user = new ControllUser
            {
                Id = 1,
                UserName = "Erik",
                Password = "password",
                Email = "mail",
                Zombies = new List<Zombie>()
            };

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, null, principal: mockedPrinicipal);
            hub.Clients.Caller.UserName = "Erik";

            hub.SignIn();

            AssertionHelper.Throws<ArgumentException>(() => hub.IsZombieOnline("some_zombie_name"));
        }

        [TestMethod]
        public void ShouldBeAbleToGetAllZombiesForUser()
        {
            var zombieList = TestingHelper.GetListOfZombies().Take(1).ToList();
            var user = new ControllUser
            {
                Id = 1,
                UserName = "Erik",
                Password = "password",
                Email = "mail",
                Zombies = zombieList
            };

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, null, principal: mockedPrinicipal);
            hub.Clients.Caller.UserName = "Erik";

            hub.SignIn();

            var result = hub.GetAllZombies().ToList();

            AssertionHelper.AssertEnumerableItemsAreEqual(user.Zombies, result, TestingHelper.ZombieViewModelComparer);
            Assert.AreEqual(zombieList.ElementAt(0).Activities.Count, result.ElementAt(0).Activities.Count());
        }

        [TestMethod]
        public void ShouldNotBeAbleToStartActivityOnZombieWhichDoesNotExistOrWhereActivityDoesNotExist()
        {;
            var user = new ControllUser
            {
                Id = 1,
                UserName = "Erik",
                Password = "password",
                Email = "mail",
                Zombies = TestingHelper.GetListOfZombies().Take(1).ToList()
            };

            user.Zombies[0].Name = "valid_zombie_name";
            user.Zombies[0].Activities[0].Name = "activity";
            user.Zombies[0].Activities[0].Id = Guid.NewGuid();
            
            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, null, principal: mockedPrinicipal);
            hub.Clients.Caller.UserName = "Erik";

            hub.SignIn();
            
            AssertionHelper.Throws<Exception>(() => hub.StartActivity("invalid_zombie_name", Guid.Empty, null, null)); // wrong name

            AssertionHelper.Throws<Exception>(() => hub.StartActivity("valid_zombie_name", Guid.NewGuid(), null, null)); // wrong guid
        }
        
        [TestMethod]
        public void ShouldBeAbleToActivateZombie()
        {
            var activity = new Activity
                {
                    Name = "activityname"
                };

            var user = new ControllUser
                {
                    Id = 1,
                    UserName = "Erik",
                    Password = "password",
                    Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    Name = "zombiename",
                                    Activities = new List<Activity>
                                        {
                                            activity
                                        }
                                }
                        }
                };
            
            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, null, principal: mockedPrinicipal);
            hub.Clients.Caller.UserName = "Erik";

            hub.SignIn();

            var dictionary = new Dictionary<string, string>
                {
                    {"param1", "param1value"}
                };
            
            hub.MockedMessageQueueService
                .Setup(x => x.InsertActivityInvocation(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname"),
                    It.Is<Dictionary<string, string>>(d => d.ContainsKey("param1") && d["param1"] == "param1value"),
                    It.Is<string>(s => s == "commandName"),
                    It.Is<string>(s => s == hub.Context.ConnectionId)))
                .Returns(new ActivityInvocationQueueItem{ Ticket = Guid.NewGuid() })
                .Verifiable();


            var ticket = hub.StartActivity("zombiename", activity.Id, dictionary, "commandName");

            Assert.AreNotEqual(Guid.Empty, ticket);

            hub.MockedMessageQueueService
                .Verify(x => x.InsertActivityInvocation(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname"),
                    It.Is<Dictionary<string, string>>(d => d.ContainsKey("param1") && d["param1"] == "param1value"),
                    It.Is<string>(s => s == "commandName"),
                    It.Is<string>(s => s == hub.Context.ConnectionId)),
                    Times.Once());
        }

        private TestableClientHub GetTestableClientHub(string connectionId, StateChangeTracker clientState, ControllUser user = null, IControllRepository controllRepository = null, ClaimsPrincipal principal = null)
        {
            // setup things needed for chat
            if (controllRepository == null)
                controllRepository = new Mock<IControllRepository>().Object;

            var connection = new Mock<IConnection>();
            
            //mocked session
            var mockedSession = new Mock<ISession>();
            mockedSession.Setup(s => s.BeginTransaction()).Returns(new Mock<ITransaction>().Object);

            // create testable chat
            var hub = new TestableClientHub(
                controllRepository,
                connection,
                new Mock<IMessageQueueService>(),
                new Mock<IActivityMessageLogService>(),
                new Mock<IMembershipService>(), 
                mockedSession);

            var mockedConnectionObject = hub.MockedConnection.Object;

            var request = new Mock<IRequest>();

            // setup signal agent
            if(principal == null)
                principal = new Mock<ClaimsPrincipal>().Object;

            request.Setup(m => m.User).Returns(principal);
            if (user != null)
                mockedSession.Setup(x => x.Get<ControllUser>(It.Is<Int32>(id => id == user.Id))).Returns(user);
            // setup client agent
            var mockPipeline = new Mock<IHubPipelineInvoker>();
            hub.Clients = new HubConnectionContext(mockPipeline.Object, mockedConnectionObject, "ClientHub", connectionId, clientState);

            // setup context
            hub.Context = new HubCallerContext(request.Object, connectionId);

            return hub;
        }

        private class TestableClientHub : ClientHub
        {
            public TestableClientHub(
                IControllRepository controllRepository,
                Mock<IConnection> connection,
                Mock<IMessageQueueService> messageQueueService,
                Mock<IActivityMessageLogService> activityService,
                Mock<IMembershipService> membershipService,
                Mock<ISession> mockedSession)
                : base(
                    controllRepository,
                    membershipService.Object,
                    messageQueueService.Object,
                    mockedSession.Object)
            {
                ControllRepository = controllRepository;
                MockedConnection = connection;
                MockedMessageQueueService = messageQueueService;
                MockedActivityService = activityService;
                MembershipService = membershipService;
                MockedSession = mockedSession;
            }

            public Mock<IConnection> MockedConnection { get; set; }
            public Mock<IMessageQueueService> MockedMessageQueueService { get; set; }
            public Mock<IActivityMessageLogService> MockedActivityService { get; set; }
            public Mock<IMembershipService> MembershipService { get; set; }
            public Mock<ISession> MockedSession { get; set; }
            public IControllRepository ControllRepository { get; set; }
        }
    }
}

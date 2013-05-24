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
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser() { UserName = "Erik", Password = "password", Id = 1 };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, principal: mockedPrinicipal);

            hub.Clients.Caller.UserName = "Erik";
            hub.SignIn();

            user = userRepository.GetByUserName(user.UserName);

            Assert.AreEqual(1, user.ConnectedClients.Count);
            Assert.AreEqual("conn-id", user.ConnectedClients[0].ConnectionId);

            hub.OnDisconnected();

            user = userRepository.GetByUserName(user.UserName);

            Assert.AreEqual(0, user.ConnectedClients.Count);
        }

        [TestMethod]
        public void ShouldBeAbleToRegisterUser()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser() { UserName = "Erik", Password = "password", };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository);
            hub.MembershipService.Setup(s => s.AddUser(It.Is<string>(u => u == "username"), It.Is<string>(p => p == "password"), It.Is<string>(e => e == "email")))
                .Returns(user)
                .Callback(() => userRepository.Add(new ControllUser{UserName = "username", Password = "password", Email = "email"}));

            hub.Clients.Caller.UserName = "username";
            var result = hub.RegisterUser("username", "password", "email");

            Assert.IsTrue(result);

            var userFromRepo = hub.ControllUserRepository.GetByUserName("username");

            Assert.AreEqual("username", userFromRepo.UserName);
            Assert.AreEqual("password", userFromRepo.Password);
            Assert.AreEqual("email", userFromRepo.Email);
        }

        [TestMethod]
        public void ShouldNotBeAbleToRegisterUser()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser() { UserName = "Erik", Password = "password", Email = "mail" };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository);
            hub.MembershipService.Setup(s => s.AddUser(It.Is<string>(u => u == "Erik"), It.IsAny<string>(), It.IsAny<string>())).Callback(() => { throw new InvalidOperationException(); });
            hub.MembershipService.Setup(s => s.AddUser(It.IsAny<string>(), It.IsAny<string>(), It.Is<string>(u => u == "mail"))).Callback(() => { throw new InvalidOperationException(); });

            hub.Clients.Caller.UserName = "username";
            AssertionHelper.Throws<InvalidOperationException>(() => hub.RegisterUser("Erik", "password", "NotSameEmail")); // Samma Username

            AssertionHelper.Throws<InvalidOperationException>(() => hub.RegisterUser("NotErik", "password", "mail")); // Samma mail
        }

        [TestMethod]
        public void ShouldBeAbleToPingZombie()
        {
            var userRepository = new InMemoryControllUserRepository();
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

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";
            
            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, principal: mockedPrinicipal);
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
            var userRepository = new InMemoryControllUserRepository();
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

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, principal: mockedPrinicipal);
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
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser
            {
                Id = 1,
                UserName = "Erik",
                Password = "password",
                Email = "mail",
                Zombies = new List<Zombie>()
            };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, principal: mockedPrinicipal);
            hub.Clients.Caller.UserName = "Erik";

            hub.SignIn();

            AssertionHelper.Throws<ArgumentException>(() => hub.IsZombieOnline("some_zombie_name"));
        }

        [TestMethod]
        public void ShouldBeAbleToGetAllZombiesForUser()
        {
            var userRepository = new InMemoryControllUserRepository();
            var zombieList = TestingHelper.GetListOfZombies().Take(1).ToList();
            var user = new ControllUser
            {
                Id = 1,
                UserName = "Erik",
                Password = "password",
                Email = "mail",
                Zombies = zombieList
            };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, principal: mockedPrinicipal);
            hub.Clients.Caller.UserName = "Erik";

            hub.SignIn();

            var result = hub.GetAllZombies().ToList();

            AssertionHelper.AssertEnumerableItemsAreEqual(user.Zombies, result, TestingHelper.ZombieViewModelComparer);
            Assert.AreEqual(zombieList.ElementAt(0).Activities.Count, result.ElementAt(0).Activities.Count());
        }

        [TestMethod]
        public void ShouldNotBeAbleToStartActivityOnZombieWhichDoesNotExistOrWhereActivityDoesNotExist()
        {
            var userRepository = new InMemoryControllUserRepository();
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

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, principal: mockedPrinicipal);
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
            var activityRepository = new InMemoryRepository<Activity>();
            activityRepository.Add(activity);

            var userRepository = new InMemoryControllUserRepository();
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

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, principal: mockedPrinicipal);
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

        private TestableClientHub GetTestableClientHub(string connectionId, StateChangeTracker clientState, ControllUser user = null, IControllUserRepository clientRepository = null, IGenericRepository<Activity> activityRepository = null, IGenericRepository<QueueItem> queueItemRepository = null, IPrincipal principal = null)
        {
            // setup things needed for chat
            if (clientRepository == null)
                clientRepository = new Mock<IControllUserRepository>().Object;

            var connection = new Mock<IConnection>();
            
            if (activityRepository == null)
                activityRepository = new Mock<IGenericRepository<Activity>>().Object;
            
            //mocked session
            var mockedSession = new Mock<ISession>();
            mockedSession.Setup(s => s.BeginTransaction()).Returns(new Mock<ITransaction>().Object);

            // create testable chat
            var hub = new TestableClientHub(
                clientRepository,
                connection,
                new Mock<IMessageQueueService>(),
                activityRepository,
                new Mock<IActivityMessageLogService>(),
                new Mock<IMembershipService>(), 
                mockedSession);

            var mockedConnectionObject = hub.MockedConnection.Object;

            var request = new Mock<IRequest>();

            // setup signal agent
            if(principal == null)
                principal = new Mock<IPrincipal>().Object;

            request.Setup(m => m.User).Returns(principal);
            mockedSession.Setup(x => x.Get<ControllUser>(It.IsAny<Int32>())).Returns((Int32 c) => clientRepository.Get(c));

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
                IControllUserRepository controllUserRepository,
                Mock<IConnection> connection,
                Mock<IMessageQueueService> messageQueueService,
                IGenericRepository<Activity> activityRepository,
                Mock<IActivityMessageLogService> activityService,
                Mock<IMembershipService> membershipService,
                Mock<ISession> mockedSession)
                : base(
                    controllUserRepository,
                    membershipService.Object,
                    messageQueueService.Object,
                    mockedSession.Object)
            {
                ControllUserRepository = controllUserRepository;
                MockedConnection = connection;
                MockedMessageQueueService = messageQueueService;
                ActivityRepository = activityRepository;
                MockedActivityService = activityService;
                MembershipService = membershipService;
                MockedSession = mockedSession;
            }

            public Mock<IConnection> MockedConnection { get; set; }
            public Mock<IMessageQueueService> MockedMessageQueueService { get; set; }
            public IGenericRepository<Activity> ActivityRepository { get; set; }
            public Mock<IActivityMessageLogService> MockedActivityService { get; set; }
            public Mock<IMembershipService> MembershipService { get; set; }
            public Mock<ISession> MockedSession { get; set; }
            public IControllUserRepository ControllUserRepository { get; set; }
        }

        /*[TestMethod]
        public void ShouldBeAbleToDownloadActivityAtZombie()
        {
            var activity = new Activity
                {
                    Name = "activityname",
                    Id = Guid.NewGuid()
                };

            var activityRepository = new InMemoryRepository<Activity>();
            activityRepository.Add(activity);

            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser
            {
                UserName = "Erik",
                Password = "password",
                ConnectedClients = new List<ControllClient>(),
                Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    ConnectionId = "zombie-conn-id",
                                    Name = "zombiename",
                                    Activities = new List<Activity>()
                                }
                        }
            };
            userRepository.Add(user);
            var clientState = new TrackingDictionary();
            const string connectionId = "conn-id";
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, activityRepository);
            hub.Clients.Caller.UserName = "Erik";

            Assert.IsTrue(hub.LogOn("password"), "Login failed");
            
            hub.MockedMessageQueueService
                .Setup(x => x.InsertActivityDownloadOrder(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname"))
                ).Verifiable();


            hub.DownloadActivityAtZombie("zombiename", activity.Id);
            
            hub.MockedMessageQueueService
                .Verify(x => x.InsertActivityDownloadOrder(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname")),
                    Times.Once());
        }*/

    }
}

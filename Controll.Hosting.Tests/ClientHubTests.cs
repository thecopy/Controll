using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using Controll.Common.ViewModels;
using Controll.Hosting.Hubs;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using FizzWare.NBuilder;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Moq;
using NHibernate;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    public class ClientHubTests
    {
        [Test]
        public void ShouldAddClientWhenLoggingIn()
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

            hub.MockedSession.Setup(x => x.Save(It.Is<ControllClient>(cc => cc.ConnectionId == hub.Context.ConnectionId))).Verifiable();

            hub.SignIn();

            hub.MockedSession.Verify(x => x.Save(It.Is<ControllClient>(cc => cc.ConnectionId == hub.Context.ConnectionId)), Times.Once());
        }
        
        [Test]
        public void ShouldBeAbleToAddZombie()
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

            hub.MockedSession.Setup(x => x.Get<ControllUser>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => user);
            hub.MockedSession.Setup(r => r.Save(It.Is<Zombie>(x => x.Name == "zombieName" && x.Owner == user))).Verifiable();

            var zombie = hub.AddZombie("zombieName");

            hub.MockedSession.Verify(r => r.Save(It.Is<Zombie>(x => x.Name == "zombieName" && x.Owner == user)), Times.Once());

            Assert.AreEqual("zombieName", zombie.Name);
        }

        [Test]
        public void ShouldBeAbleToGetLogs()
        {
            var mockedRepository = new Mock<IControllRepository>();
            var user = new ControllUser
                {
                    UserName = "Erik",
                    Password = "password",
                    Id = 1,
                    LogBooks = new List<LogBook>
                        {
                            Builder<LogBook>.CreateNew()
                                            .With(x => x.Activity = Builder<Activity>.CreateNew().Build())
                                            .And(x => x.LogMessages = new List<LogMessage>
                                                {
                                                    Builder<LogMessage>.CreateNew().Build()
                                                })
                                            .Build()
                        }
                };

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));

            var hub = GetTestableClientHub(connectionId, clientState, user, mockedRepository.Object, mockedPrinicipal);

            hub.MockedSession.Setup(x => x.Get<ControllUser>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => user);

            var books = hub.GetLogBooks(10, 0).ToList();

            Assert.AreEqual(1, books.Count());
            Assert.AreEqual(1, books.ElementAt(0).Messages.Count());
        }

        [Test]
        public void ShouldNotBeAbleToAddZombieIfNameAlreadyExists()
        {
            var user = new ControllUser()
            {
                UserName = "user",
                Zombies = new List<Zombie> { new Zombie { Name = "zombieName" } },
                Id = 1
            };

            var mockedRepository = new Mock<IControllRepository>();

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var mockedPrinicipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, "1"),
                }, Constants.ControllAuthType));

            var hub = GetTestableClientHub(connectionId, clientState, user, mockedRepository.Object, principal: mockedPrinicipal);

            hub.MockedSession.Setup(x => x.Load<ControllUser>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => user);

            Assert.Throws<InvalidOperationException>(() => hub.AddZombie("zombieName"));
        }
        
        [Test]
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

            hub.MockedControllService
                .Setup(x => x.InsertPingMessage(It.Is<Zombie>(z => z == user.Zombies[0]), It.Is<String>(s => s == hub.Context.ConnectionId)))
                .Returns(new PingQueueItem { Ticket = Guid.NewGuid() }).Verifiable("InsertPingMessage was not called by hub");

            var ticket = hub.PingZombie("zombie");

            Assert.AreNotEqual(Guid.Empty, ticket);
            hub.MockedControllService.Verify(x => x.InsertPingMessage(It.Is<Zombie>(z => z == user.Zombies[0]), It.Is<String>(s => s == hub.Context.ConnectionId)), Times.Once());
        }
        
        [Test]
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
            Assert.True(result);

            user.Zombies[0].ConnectedClients[0].ConnectionId = null;
            result = hub.IsZombieOnline("zombie");

            Assert.False(result);

            user.Zombies[0].ConnectedClients.Clear();
            result = hub.IsZombieOnline("zombie");

            Assert.False(result);
        }


        [Test]
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

            Assert.Throws<ArgumentException>(() => hub.IsZombieOnline("some_zombie_name"));
        }

        [Test]
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

        [Test]
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
            
            Assert.Throws<Exception>(() => hub.StartActivity("invalid_zombie_name", Guid.Empty, null, null)); // wrong name

            Assert.Throws<Exception>(() => hub.StartActivity("valid_zombie_name", Guid.NewGuid(), null, null)); // wrong guid
        }
        
        [Test]
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
            
            hub.MockedControllService
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

            hub.MockedControllService
                .Verify(x => x.InsertActivityInvocation(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname"),
                    It.Is<Dictionary<string, string>>(d => d.ContainsKey("param1") && d["param1"] == "param1value"),
                    It.Is<string>(s => s == "commandName"),
                    It.Is<string>(s => s == hub.Context.ConnectionId)),
                    Times.Once());
        }

        [Test]
        public void ShouldBeAbleToSendDownloadAcitivityMessage()
        {
            var user = new ControllUser
            {
                Id = 1,
                UserName = "Erik",
                Password = "password",
                Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    Name = "zombiename"
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

            const string downloadUrl = @"http://download";

            hub.MockedControllService
                .Setup(x => x.InsertActivityDownload(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<string>(s => s == downloadUrl)))
                .Returns(new ActivityInvocationQueueItem { Ticket = Guid.NewGuid() })
                .Verifiable();


            var ticket = hub.DownloadActivity(user.Zombies.ElementAt(0).Name, downloadUrl);

            Assert.AreNotEqual(Guid.Empty, ticket);

            hub.MockedControllService
                .Verify(x => x.InsertActivityDownload(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<string>(s => s == downloadUrl)),
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
                new Mock<IControllService>(),
                new Mock<IMembershipService>(), 
                mockedSession,
                new Mock<IDispatcher>());

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
                Mock<IControllService> controllService,
                Mock<IMembershipService> membershipService,
                Mock<ISession> mockedSession,
                Mock<IDispatcher> mockedDispatcher)
                : base(
                    controllRepository, controllService.Object,
                    mockedDispatcher.Object,
                    mockedSession.Object)
            {
                ControllRepository = controllRepository;
                MockedConnection = connection;
                MockedControllService = controllService;
                MembershipService = membershipService;
                MockedSession = mockedSession;
                MockedDispatcher = mockedDispatcher;
            }

            public Mock<IConnection> MockedConnection { get; set; }
            public Mock<IControllService> MockedControllService { get; set; }
            public Mock<IMembershipService> MembershipService { get; set; }
            public Mock<ISession> MockedSession { get; set; }
            public Mock<IDispatcher> MockedDispatcher { get; set; }
            public new IControllRepository ControllRepository { get; set; }
        }
    }
}

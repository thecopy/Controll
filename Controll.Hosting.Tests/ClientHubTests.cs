using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Principal;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Hubs;
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
        public void ShouldNotBeAbleToAuthenticateWithWrongPassword()
        {
            var clientState = new StateChangeTracker();
            const string clientId = "1";

            var user = new ControllUser
                {
                    UserName = "Erik",
                    Password = "pass123"
                };

            var repo = new InMemoryControllUserRepository();
            repo.Add(user);

            var hub = GetTestableClientHub(clientId, clientState,user, repo);
            hub.Clients.Caller.UserName = "Erik";

            Assert.IsFalse(hub.LogOn("pass")); // Wrong password
            hub.Clients.Caller.UserName = "Erika123";
            Assert.IsFalse(hub.LogOn("pass123")); // Wrong username

        }

        [TestMethod]
        public void ShouldBeAbleToAuthenticate()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser() { UserName = "Erik", Password = "password", ConnectedClients = new List<ControllClient>() };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string clientId = "1";

            var hub = GetTestableClientHub(clientId, clientState, user, userRepository);
            hub.Clients.Caller.UserName = "Erik";

            hub.LogOn("password");
        }

        [TestMethod]
        public void ShouldBeAbleToDetectUserNameSpoof()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser() { UserName = "Erik", Password = "password", ConnectedClients = new List<ControllClient>() };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string clientId = "1";

            var hub = GetTestableClientHub(clientId, clientState, user, userRepository);
            hub.Clients.Caller.UserName = "Erik";

            // Not logged in.
            var ticket =  hub.StartActivity("zombieName", Guid.NewGuid(), parameters: null, commandName: null);
            Assert.AreEqual(default(Guid), ticket);
        }

       
        [TestMethod]
        public void ShouldBeAbleGetAllInstalledPluginsOnZombie()
        {
            var userRepository = new InMemoryControllUserRepository();
            var activities = Builder<Activity>.CreateListOfSize(10).All().Do(a => a.Commands = new List<ActivityCommand>()).Build();
            var user = new ControllUser
                {
                    UserName = "Erik",
                    Password = "password",
                    ConnectedClients = new List<ControllClient>(),
                    Zombies = new List<Zombie>
                        {
                            new Zombie {Activities = activities, Name = "zombie"}
                        }
                };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string clientId = "1";

            var hub = GetTestableClientHub(clientId, clientState, user, userRepository);
            hub.Clients.Caller.UserName = "Erik";
            
            hub.LogOn("password");
            List<ActivityViewModel> fetchedActivities = hub.GetActivitesInstalledOnZombie("zombie").ToList();

            Assert.IsNotNull(fetchedActivities);
            AssertionHelper.AssertEnumerableItemsAreEqual(user.Zombies[0].Activities, fetchedActivities,
                                                          TestingHelper.ActivityViewModelComparer);
        }

        [TestMethod]
        public void ShouldBeAbleToAddClientWhenLoggingInAndRemoveClientFromUserWhenDisconnecting()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser() { UserName = "Erik", Password = "password", ConnectedClients = new List<ControllClient>()};

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository);
            hub.Clients.Caller.UserName = "Erik";

            hub.LogOn("password");

            user = userRepository.GetByUserName(user.UserName);

            Assert.AreEqual(1, user.ConnectedClients.Count);
            Assert.AreEqual("conn-id", user.ConnectedClients[0].ConnectionId);

            hub.OnDisconnect();

            user = userRepository.GetByUserName(user.UserName);

            Assert.AreEqual(0, user.ConnectedClients.Count);
        }

        [TestMethod]
        public void ShouldBeAbleToRegisterUser()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser() { UserName = "Erik", Password = "password", ConnectedClients = new List<ControllClient>() };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository);

            hub.Clients.Caller.UserName = "username";
            var result = hub.RegisterUser("username", "password", "email");

            Assert.IsTrue(result);

            var userFromRepo = hub.ControllUserRepository.GetByUserName("username");

            Assert.AreEqual("username", userFromRepo.UserName);
            Assert.AreEqual("password", userFromRepo.Password);
            Assert.AreEqual("email", userFromRepo.EMail);
        }

        [TestMethod]
        public void ShouldNotBeAbleToRegisterUser()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser() { UserName = "Erik", Password = "password", EMail = "mail", ConnectedClients = new List<ControllClient>() };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository);

            hub.Clients.Caller.UserName = "username";
            var result = hub.RegisterUser("Erik", "password", "NotSameEmail"); // Samma Username

            Assert.IsFalse(result);
             
            result = hub.RegisterUser("NotErik", "password", "mail"); // Samma mail

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldBeAbleToPingZombie()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser
            {
                UserName = "Erik",
                Password = "password",
                EMail = "mail",
                ConnectedClients = new List<ControllClient>(),
                Zombies = new List<Zombie> { new Zombie { Name = "zombie", ConnectionId = "connection-id" } }
            };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository);

            hub.Clients.Caller.UserName = "Erik";
            hub.LogOn("password");

            hub.MockedMessageQueueService.Setup(x => x.InsertPingMessage(It.Is<Zombie>(z => z == user.Zombies[0]), It.Is<String>(s => s == hub.Context.ConnectionId))).Returns(Guid.NewGuid).Verifiable("InsertPingMessage was not called by hub");

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
                    UserName = "Erik",
                    Password = "password",
                    EMail = "mail",
                    ConnectedClients = new List<ControllClient>(),
                    Zombies = new List<Zombie> {new Zombie {Name = "zombie", ConnectionId = "connection-id"}}
                };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            TestableClientHub hub = GetTestableClientHub(connectionId, clientState, user, userRepository);

            hub.Clients.Caller.UserName = "Erik";
             hub.LogOn("password");

            var result = hub.IsZombieOnline("zombie");
            Assert.IsTrue(result);

            user.Zombies[0].ConnectionId = null;
            result = hub.IsZombieOnline("zombie");

            Assert.IsFalse(result);
        }


        [TestMethod]
        public void ShouldThrowWhenGettingZombieOnlineStatusOnNonExistingZombie()
        {
            var userRepository = new InMemoryControllUserRepository();
            var user = new ControllUser
            {
                UserName = "Erik",
                Password = "password",
                EMail = "mail",
                ConnectedClients = new List<ControllClient>(),
                Zombies = new List<Zombie>()
            };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            TestableClientHub hub = GetTestableClientHub(connectionId, clientState, user, userRepository);

            hub.Clients.Caller.UserName = "Erik";
            hub.LogOn("password");

            AssertionHelper.Throws<ArgumentException>(() => hub.IsZombieOnline("some_zombie_name"));
        }

        [TestMethod]
        public void ShouldBeAbleToGetAllZombiesForUser()
        {
            var userRepository = new InMemoryControllUserRepository();
            var zombieList = TestingHelper.GetListOfZombies().Take(1).ToList();
            var user = new ControllUser
            {
                UserName = "Erik",
                Password = "password",
                EMail = "mail",
                ConnectedClients = new List<ControllClient>(),
                Zombies = zombieList
            };

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository);

            hub.Clients.Caller.UserName = "Erik";
            hub.LogOn("password");

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
                UserName = "Erik",
                Password = "password",
                EMail = "mail",
                ConnectedClients = new List<ControllClient>(),
                Zombies = TestingHelper.GetListOfZombies().Take(1).ToList()
            };

            user.Zombies[0].Name = "valid_zombie_name";
            user.Zombies[0].Activities[0].Name = "activity";
            user.Zombies[0].Activities[0].Id = Guid.NewGuid();

            userRepository.Add(user);

            var clientState = new StateChangeTracker();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository);

            hub.Clients.Caller.UserName = "Erik";
            hub.LogOn("password");

            Assert.AreEqual(default(Guid), hub.StartActivity("invalid_zombie_name", Guid.Empty, null, null)); // wrong name

            Assert.AreEqual(default(Guid), hub.StartActivity("valid_zombie_name", Guid.NewGuid(), null, null)); // wrong guid
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
                    UserName = "Erik",
                    Password = "password",
                    ConnectedClients = new List<ControllClient>(),
                    Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    ConnectionId = "zombie-conn-id",
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
            var hub = GetTestableClientHub(connectionId, clientState, user, userRepository, activityRepository);
            hub.Clients.Caller.UserName = "Erik";
            hub.LogOn("password");

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
                .Returns(Guid.NewGuid())
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

        [TestMethod]
        public void ShouldNotBeAbleToInvokeMethodsWhichRequireAuthenticationWhenNotAuthenticated()
        {
            var clientState = new StateChangeTracker();
            const string clientId = "1";

            var user = new ControllUser
            {
                UserName = "Erik",
                Password = "pass123",
                ConnectedClients = new List<ControllClient>()
            };

            var repo = new InMemoryControllUserRepository();
            repo.Add(user);

            var hub = GetTestableClientHub(clientId, clientState, user, repo);
            hub.Clients.Caller.UserName = "Erik";

            var methods = typeof (ClientHub).GetMethods()
                              .Where(y => y.GetCustomAttributes().OfType<RequiresAuthorization>().Any());
            
            foreach (var method in methods)
            {
                var parameterInfos = method.GetParameters();
                var parameters = new object[parameterInfos.Count()];
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var paramter = parameterInfos[i];
                    var value = paramter.GetType().IsValueType ? Activator.CreateInstance(paramter.GetType()) : null;

                    parameters[i] = value;
                }
                var closureSafeMethod = method;

                if (typeof(IEnumerable).IsAssignableFrom(closureSafeMethod.ReturnType))
                {
                    IList<object> returned = ((IEnumerable<object>)closureSafeMethod.Invoke(hub, parameters)).ToList(); // .ToList() -> forcing enumeration (and therefore execution) for IEnumerable return types
                    Assert.AreEqual(0, returned.Count);
                }
                else
                {
                    var returned = closureSafeMethod.Invoke(hub, parameters);
                    var expected = closureSafeMethod.ReturnType.IsValueType ? Activator.CreateInstance(returned.GetType()) : null;
                    Assert.AreEqual(expected, returned);
                }

                #region This Should Be Used when SignalR implements exception bubbling

                /*
                // Checking InnerException because MethodInvocationException would be checked otherwise
                if (typeof (IEnumerable).IsAssignableFrom(closureSafeMethod.ReturnType))
                {
                    AssertionHelper.Throws<AuthenticationException>(() =>
                        ((IEnumerable<object>) closureSafeMethod.Invoke(hub, parameters)).ToList(), innerException:true); // .ToList() -> forcing enumeration (and therefore execution) for IEnumerable return types
                }
                else
                {
                    AssertionHelper.Throws<AuthenticationException>(() => closureSafeMethod.Invoke(hub, parameters), innerException:true);
                }
 * */

                #endregion
            }
        }

        private TestableClientHub GetTestableClientHub(string connectionId, StateChangeTracker clientState, ControllUser user = null, IControllUserRepository clientRepository = null, IGenericRepository<Activity> activityRepository = null, IGenericRepository<QueueItem> queueItemRepository = null)
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
                new Mock<IActivityService>(),
                mockedSession);

            var mockedConnectionObject = hub.MockedConnection.Object;


            // setup signal agent
            var prinicipal = new Mock<IPrincipal>();

            var request = new Mock<IRequest>();
            request.Setup(m => m.User).Returns(prinicipal.Object);

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
                Mock<IActivityService> activityService,
                Mock<ISession> mockedSession)
                : base(
                    controllUserRepository,
                    messageQueueService.Object,
                    mockedSession.Object)
            {
                ControllUserRepository = controllUserRepository;
                MockedConnection = connection;
                MockedMessageQueueService = messageQueueService;
                ActivityRepository = activityRepository;
                MockedActivityService = activityService;
                MockedSession = mockedSession;
            }

            public Mock<IConnection> MockedConnection { get; set; }
            public Mock<IMessageQueueService> MockedMessageQueueService { get; set; }
            public IGenericRepository<Activity> ActivityRepository { get; set; }
            public Mock<IActivityService> MockedActivityService { get; set; }
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

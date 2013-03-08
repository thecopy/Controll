using System;
using System.Collections.Generic;
using System.Security.Principal;
using Controll.Common;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NHibernate;
using SignalR;
using SignalR.Hubs;
using ParameterDescriptor = Controll.Common.ParameterDescriptor;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class ClientHubTests
    {
        private static ControllUser _user;
        private static Activity _activity;
        private readonly ISessionFactory _sessionFactory = NHibernateHelper.GetSessionFactoryForMockedData();
        private ControllUserRepository _userRepository;
        private GenericRepository<Activity> _activityRepository;

        // This will be saved later in some test-env deployment-script of some sort or something. Or this whole class may be refactored. Anyway, keep this for safe storage 
#if FALSE
        public static void Init(TestContext context)
        {
            Console.WriteLine("Initialize");

            //
            //    
            //
            //var session = NHibernateHelper.GetSessionFactoryForMockedData().OpenSession();
            //using(session)
            //using(var trans = session.BeginTransaction())
            //{
            //    var _userRepository = new ControllUserRepository(session);
            //    var _activityRepository = new GenericRepository<Activity>(session);
            //    _user = new ControllUser
            //        {
            //            UserName = "Erik",
            //            EMail = "mail",
            //            Password = "password",
            //            ConnectedClients = new List<ControllClient>(),
            //            Zombies = new List<Zombie>
            //                {
            //                    new Zombie
            //                        {
            //                            ConnectionId = "zombie-conn-id",
            //                            Name = "zombiename",
            //                            Activities = new List<Activity>
            //                                {
            //                                    _activity
            //                                }
            //                        }
            //                }
            //        };

            //    _activity = new Activity
            //        {
            //            Name = "activityname",
            //            CreatorName = "creatorname",
            //            Description = "description",
            //            FilePath = "dummypath.dll",
            //            LastUpdated = DateTime.UtcNow,
            //            Version = new Version(1, 0),
            //            Commands = new List<ActivityCommand>
            //                {
            //                    new ActivityCommand
            //                        {
            //                            IsQuickCommand = false,
            //                            Label = "commandlabel",
            //                            Name = "commandname",
            //                            ParameterDescriptors = new List<ParameterDescriptor>
            //                                {
            //                                    new ParameterDescriptor
            //                                        {
            //                                            Description = "parameterdescription",
            //                                            Label = "parameterlabel",
            //                                            Name = "parametername"
            //                                        }
            //                                }
            //                        }
            //                }
            //        };

            //    _userRepository.Add(_user);
            //    _activityRepository.Add(_activity);
                
            //    trans.Commit();
            //}
        }
#endif

        [TestMethod]
        public void ShouldNotBeAbleToAuthenticateWithWrongPassword()
        {
            var clientState = new TrackingDictionary();
            const string clientId = "1";

            var hub = GetTestableClientHub(clientId, clientState);
            hub.Caller.UserName = "Erik";

            bool result = hub.LogOn("pass");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldNotBeAbleToAuthenticateWitNonExistantUser()
        {
            var clientState = new TrackingDictionary();
            const string clientId = "1";

            var hub = GetTestableClientHub(clientId, clientState);
            hub.Caller.UserName = "AnotherUser";

            bool result = hub.LogOn("pass");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldBeAbleToAuthenticate()
        {
            var clientState = new TrackingDictionary();
            const string clientId = "1";

            var hub = GetTestableClientHub(clientId, clientState);
            hub.Caller.UserName = "Erik";

            bool result = hub.LogOn("password");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ShouldBeAbleToAddClientWhenLoggingInAndRemoveClientFromUserWhenDisconnecting()
        {
            _user = _userRepository.GetByUserName(_user.UserName);
            _user.ConnectedClients.Clear();
            _userRepository.Update(_user);

            var clientState = new TrackingDictionary();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState);
            hub.Caller.UserName = "Erik";

            hub.LogOn("password");

            _user = _userRepository.GetByUserName(_user.UserName);

            Assert.AreEqual(1, _user.ConnectedClients.Count);
            Assert.AreEqual("conn-id", _user.ConnectedClients[0].ConnectionId);

            hub.Disconnect();

            _user = _userRepository.GetByUserName(_user.UserName);

            Assert.AreEqual(0, _user.ConnectedClients.Count);
        }

        [TestMethod]
        public void ShouldBeAbleToRegisterUser()
        {
            var clientState = new TrackingDictionary();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState);

            hub.Caller.UserName = "username";
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
            var clientState = new TrackingDictionary();
            const string connectionId = "conn-id";

            var hub = GetTestableClientHub(connectionId, clientState);

            hub.Caller.UserName = "username";
            var result = hub.RegisterUser("Erik", "password", "NotSameEmail"); // Samma Username

            Assert.IsFalse(result);
             
            result = hub.RegisterUser("NotErik", "password", "mail"); // Samma mail

            Assert.IsFalse(result);
        }
        
        [TestMethod]
        public void ShouldBeAbleToActivateZombie()
        {
            var clientState = new TrackingDictionary();
            const string connectionId = "conn-id";
            var hub = GetTestableClientHub(connectionId, clientState);
            hub.Caller.UserName = "Erik";

            hub.LogOn("password");
            
            _user.Zombies = new List<Zombie>
                {
                    new Zombie
                        {
                            ConnectionId = "zombie-conn-id",
                            Name = "zombiename",
                            Activities = new List<Activity>
                                {
                                    _activity
                                }
                        }
                };

            _userRepository.Update(_user);

            var dictionary = new Dictionary<string, string>
                {
                    {"param1", "param1value"}
                };

            hub.MockedMessageQueueService
                .Setup(x => x.InsertActivityInvocation(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname"),
                    It.Is<Dictionary<string, string>>(d => d.ContainsKey("param1") && d["param1"] == "param1value"),
                    It.Is<string>(s => s == "commandname"))
                ).Verifiable();

            hub.StartActivity("zombiename", _activity.Id, dictionary, "commandname");

            hub.MockedMessageQueueService
                .Verify(x => x.InsertActivityInvocation(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname"),
                    It.Is<Dictionary<string, string>>(d => d.ContainsKey("param1") && d["param1"] == "param1value"),
                    It.Is<string>(s => s == "commandname")),
                    Times.Once());

            _user.Zombies[0].Activities.Clear();
            _userRepository.Update(_user);

        }

        [TestMethod]
        public void ShouldBeAbleToDownloadActivityAtZombie()
        {
            var clientState = new TrackingDictionary();
            const string connectionId = "conn-id";
            var hub = GetTestableClientHub(connectionId, clientState);
            hub.Caller.UserName = "Erik";

            hub.LogOn("password");
            
            _user.Zombies = new List<Zombie>{
                    new Zombie
                        {
                            ConnectionId = "zombie-conn-id",
                            Name = "zombiename",
                            Activities = new List<Activity>()
                        }};

            _userRepository.Update(_user);
            
            hub.MockedMessageQueueService
                .Setup(x => x.InsertActivityDownloadOrder(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname"))
                ).Verifiable();


            hub.DownloadActivityAtZombie("zombiename", _activity.Id);
            
            hub.MockedMessageQueueService
                .Verify(x => x.InsertActivityDownloadOrder(
                    It.Is<Zombie>(z => z.Name == "zombiename"),
                    It.Is<Activity>(a => a.Name == "activityname")),
                    Times.Once());
        }

        private TestableClientHub GetTestableClientHub(string connectionId, TrackingDictionary clientState, ControllUser user = null, IControllUserRepository clientRepository = null)
        {
            // setup things needed for chat
            if(clientRepository == null)
                clientRepository = new ControllUserRepository(NHibernateHelper.GetSessionFactoryForMockedData().OpenSession());

            var connection = new Mock<IConnection>();

            // add user to repository
            if (user != null)
                clientRepository.Add(user);

            var genericActivityRepository =
                new GenericRepository<Activity>(NHibernateHelper.GetSessionFactoryForMockedData().OpenSession());
            var mockedMessageQueueService =
                new Mock<IMessageQueueService>();

            // create testable chat
            var hub = new TestableClientHub(
                clientRepository,
                connection,
                mockedMessageQueueService,
                genericActivityRepository,
                new Mock<IActivityService>());

            var mockedConnectionObject = hub.MockedConnection.Object;

            // setup client agent
            hub.Clients = new ClientAgent(mockedConnectionObject, "PcHub");

            // setup signal agent
            var prinicipal = new Mock<IPrincipal>();

            var request = new Mock<IRequest>();
            request.Setup(m => m.User).Returns(prinicipal.Object);

            hub.Caller = new StatefulSignalAgent(mockedConnectionObject, connectionId, "PcHub", clientState);

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
                Mock<IActivityService> activityService)
                : base(
                    controllUserRepository,
                    messageQueueService.Object,
                    activityRepository,
                    activityService.Object)
            {
                ControllUserRepository = controllUserRepository;
                MockedConnection = connection;
                MockedMessageQueueService = messageQueueService;
                ActivityRepository = activityRepository;
                MockedActivityService = activityService;
            }

            public Mock<IConnection> MockedConnection { get; set; }
            public Mock<IMessageQueueService> MockedMessageQueueService { get; set; }
            public IGenericRepository<Activity> ActivityRepository { get; set; }
            public Mock<IActivityService> MockedActivityService { get; set; }
            public IControllUserRepository ControllUserRepository { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NHibernate;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class ZombieHubTests
    {
        [TestMethod]
        public void ShouldBeAbleToAuthenticate()
        {
            var hub = GetTestableZombieHub();
            hub.Clients.Caller.ZombieName = "zombie";
            hub.Clients.Caller.BelongsToUser = "username";

            var user = new ControllUser
                {
                    UserName = "username",
                    Password = "password",
                    Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    Name = "zombie",
                                    Id = 1
                                }
                        }
                };

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);

            hub.MockedUserRepository.Setup(r => r.Update(It.Is<ControllUser>(x => x.Zombies[0].ConnectionId == hub.Context.ConnectionId))).Verifiable();

            bool result = hub.LogOn("password");
            Assert.IsTrue(result);

            // Verify that connection-id was set
            hub.MockedUserRepository.Verify(r => r.Update(It.Is<ControllUser>(x => x.Zombies[0].ConnectionId == hub.Context.ConnectionId)), Times.Once());
        }

        [TestMethod]
        public void ShouldNotBeAbleToAuthenticate()
        {
            var hub = GetTestableZombieHub();

            var user = new ControllUser
            {
                UserName = "username",
                Password = "password",
                Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    Name = "zombie",
                                    Id = 1
                                }
                        }
            };

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);

            hub.Clients.Caller.ZombieName = "wrong_zombie";
            hub.Clients.Caller.BelongsToUser = "wrong_username";
            AssertionHelper.Throws<AuthenticationException>(() => hub.LogOn("password")); // Username wrong but correct password
            AssertionHelper.Throws<AuthenticationException>(() => hub.LogOn("wrong_password")); // Username wrong and incorrect password

            hub.Clients.Caller.BelongsToUser = "username";
            AssertionHelper.Throws<AuthenticationException>(() => hub.LogOn("wrong_password")); // Username correct but incorrect password
            AssertionHelper.Throws<ArgumentException>(() => hub.LogOn("password"));       // Username correct and correct password but zombie name wrong
        }

        [TestMethod]
        public void ShouldBeAbleToSetMessageAsDelivered()
        {
            var hub = GetTestableZombieHub();
            hub.Clients.Caller.ZombieName = "zombie";
            hub.Clients.Caller.BelongsToUser = "username";

            var user = new ControllUser
            {
                UserName = "username",
                Password = "password",
                Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    Name = "zombie",
                                    Id = 1
                                }
                        }
            };

            var ticket = Guid.NewGuid();

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);
            hub.MockedMessageQueueService.Setup(x => x.MarkQueueItemAsDelivered(It.Is<Guid>(guid => guid.Equals(ticket)))).Verifiable();

            hub.LogOn("password");
            hub.QueueItemDelivered(ticket);

            hub.MockedMessageQueueService.Verify(x => x.MarkQueueItemAsDelivered(It.Is<Guid>(guid => guid.Equals(ticket))));
        }

        [TestMethod]
        public void ShouldBeAbleToThrowExceptionWhenNotAuthenticated()
        {
            var hub = GetTestableZombieHub();

            var user = new ControllUser
            {
                UserName = "username",
                Password = "password",
                Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    Name = "zombie",
                                    Id = 1
                                }
                        }
            };

            var ticket = Guid.NewGuid();

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);
            hub.MockedMessageQueueService.Setup(x => x.MarkQueueItemAsDelivered(It.Is<Guid>(guid => guid.Equals(ticket)))).Verifiable();


            AssertionHelper.Throws<AuthenticationException>(() => hub.QueueItemDelivered(ticket)); // Not Logged On
            hub.Clients.Caller.BelongsToUser = "username";
            hub.Clients.Caller.ZombieName = "zombie";
            hub.LogOn("password");

            hub.Clients.Caller.BelongsToUser = "someone_else"; // Spoofing username
            AssertionHelper.Throws<AuthenticationException>(() => hub.QueueItemDelivered(ticket)); // Spoofing username

            hub.Clients.Caller.BelongsToUser = "username"; // Correct Username
            hub.Clients.Caller.ZombieName = "another_zombie"; // Spoofing zombie
            AssertionHelper.Throws<AuthenticationException>(() => hub.QueueItemDelivered(ticket)); // Spoofing zombie

            hub.Clients.Caller.ZombieName = "zombie"; // Correct Zombie
            user.Zombies[0].ConnectionId = "another_connection_id"; // Wrong Connection-ID
            AssertionHelper.Throws<AuthenticationException>(() => hub.QueueItemDelivered(ticket)); // Wrong Connection-ID
        }

        [TestMethod]
        public void ShouldBeAbleToCleanUpAfterZombieDisconnect()
        {
            var hub = GetTestableZombieHub();
            hub.Clients.Caller.ZombieName = "zombie";
            hub.Clients.Caller.BelongsToUser = "username";

            var user = new ControllUser
                {
                    UserName = "username",
                    Password = "password",
                    Zombies = new List<Zombie>
                        {
                            new Zombie
                                {
                                    Name = "zombie",
                                    Id = 1,
                                    ConnectionId = hub.Context.ConnectionId // Fake Having Logged In
                                }
                        }
                };

            hub.MockedUserRepository.Setup(x => x.GetByUserName(It.Is<string>(s => s == "username")))
                .Returns(user)
                .Verifiable();
            hub.MockedUserRepository.Setup(r => r.Update(It.Is<ControllUser>(x => x.Zombies[0].ConnectionId == null)))
                .Verifiable();

            hub.OnDisconnect();

            hub.MockedUserRepository.Verify(x => x.GetByUserName(It.Is<string>(s => s == "username")), Times.Once());
            hub.MockedUserRepository.Verify(r => r.Update(It.Is<ControllUser>(x => x.Zombies[0].ConnectionId == null)), Times.Once());
        }
        
        [TestMethod]
        public void ShouldBeAbleToRegisterAsZombie()
        {
            var hub = GetTestableZombieHub();

            hub.Clients.Caller.BelongsToUser = "Erik";
            hub.Clients.Caller.ZombieName = "ZombieName";

            var user = new ControllUser
                {
                    UserName = "Erik",
                    Password = "password",
                    Zombies = new List<Zombie>()
                };

            hub.MockedUserRepository.Setup(x => x.GetByUserName("Erik")).Returns(user);
            
            hub.MockedUserRepository.Setup(x => x.Update(It.Is<ControllUser>(u => u.Zombies.Any(z => z.Name == "ZombieName")))).Verifiable();

            var result = hub.RegisterAsZombie("password");

            hub.MockedUserRepository.Verify(x => x.Update(It.Is<ControllUser>(u => u.Zombies.Any(z => z.Name == "ZombieName"))));

            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public void ShouldNotBeAbleToRegisterAsZombie()
        {
            var hub = GetTestableZombieHub();
            var user = new ControllUser
            {
                UserName = "username",
                Password = "password",
                Zombies = new List<Zombie>
                    {
                        new Zombie
                            {
                                Name = "existing_zombie"
                            }
                    }
            };

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);

            hub.Clients.Caller.ZombieName = "existing_zombie";
            hub.Clients.Caller.BelongsToUser = "wrong_username";
            AssertionHelper.Throws<AuthenticationException>(() => hub.RegisterAsZombie("password")); // Username wrong but correct password
            AssertionHelper.Throws<AuthenticationException>(() => hub.RegisterAsZombie("wrong_password")); // Username wrong and incorrect password

            hub.Clients.Caller.BelongsToUser = "username";
            AssertionHelper.Throws<AuthenticationException>(() => hub.RegisterAsZombie("wrong_password")); // Username correct but incorrect password
            AssertionHelper.Throws<ArgumentException>(() => hub.RegisterAsZombie("password"));             // Username correct and correct password but zombie name already existing
        }

        private TestableZombieHub GetTestableZombieHub()
        {
            var mockedActivityService = new Mock<IActivityService>();
            var mockedUserRepository = new Mock<IControllUserRepository>();
            var mockedActivityRepository = new Mock<IGenericRepository<Activity>>();
            var mockedMessageQueueService = new Mock<IMessageQueueService>();
            var mockPipeline = new Mock<IHubPipelineInvoker>();
            var mockedConnectionObject = new Mock<IConnection>();
            var mockedSession = new Mock<ISession>();
            mockedSession.Setup(s => s.BeginTransaction()).Returns(new Mock<ITransaction>().Object);
           

            var hub = new TestableZombieHub(mockedUserRepository, mockedActivityService, mockedActivityRepository, mockedMessageQueueService, mockedSession)
                {
                    Clients = new HubConnectionContext(mockPipeline.Object, mockedConnectionObject.Object, "ZombieHub", "conn-id", new StateChangeTracker()),
                    Context = new HubCallerContext(new Mock<IRequest>().Object, "connid")
                };
            
            return hub;
        }

        private class TestableZombieHub : ZombieHub
        {
            public Mock<IControllUserRepository> MockedUserRepository { get; set; }
            public Mock<IActivityService> MockedActivityService { get; set; }
            public Mock<IGenericRepository<Activity>> MockedActivityRepository { get; set; }
            public Mock<IMessageQueueService> MockedMessageQueueService { get; set; }
            public Mock<ISession> MockedSession { get; set; }

            public TestableZombieHub(
                Mock<IControllUserRepository> mockedUserRepository, 
                Mock<IActivityService> mockedActivityService, 
                Mock<IGenericRepository<Activity>> mockedActivityRepository,
                Mock<IMessageQueueService> mockedMessageQueueService,
                Mock<ISession> mockedSession)
                : base(mockedUserRepository.Object, mockedActivityService.Object, mockedActivityRepository.Object, mockedMessageQueueService.Object, mockedSession.Object)
            {
                MockedUserRepository = mockedUserRepository;
                MockedActivityService = mockedActivityService;
                MockedActivityRepository = mockedActivityRepository;
                MockedMessageQueueService = mockedMessageQueueService;
                MockedSession = mockedSession;
            }
        }
    }
}

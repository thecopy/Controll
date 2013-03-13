using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SignalR;
using SignalR.Hubs;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class ZombieHubTests
    {
        [TestMethod]
        public void ShouldBeAbleToAuthenticate()
        {
            var hub = GetTestableZombieHub();
            hub.Caller.ZombieName = "zombie";
            hub.Caller.BelongsToUser = "username";

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

            hub.Caller.ZombieName = "wrong_zombie";
            hub.Caller.BelongsToUser = "wrong_username";
            AssertionHelper.Throws<AuthenticationException>(() => hub.LogOn("password")); // Username wrong but correct password
            AssertionHelper.Throws<AuthenticationException>(() => hub.LogOn("wrong_password")); // Username wrong and incorrect password

            hub.Caller.BelongsToUser = "username";
            AssertionHelper.Throws<AuthenticationException>(() => hub.LogOn("wrong_password")); // Username correct but incorrect password
            AssertionHelper.Throws<ArgumentException>(() => hub.LogOn("password"));       // Username correct and correct password but zombie name wrong
        }

        [TestMethod]
        public void ShouldBeAbleToSetMessageAsDelivered()
        {
            var hub = GetTestableZombieHub();
            hub.Caller.ZombieName = "zombie";
            hub.Caller.BelongsToUser = "username";

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
            hub.Caller.BelongsToUser = "username";
            hub.Caller.ZombieName = "zombie";
            hub.LogOn("password");

            hub.Caller.BelongsToUser = "someone_else"; // Spoofing username
            AssertionHelper.Throws<AuthenticationException>(() => hub.QueueItemDelivered(ticket)); // Spoofing username

            hub.Caller.BelongsToUser = "username"; // Correct Username
            hub.Caller.ZombieName = "another_zombie"; // Spoofing zombie
            AssertionHelper.Throws<AuthenticationException>(() => hub.QueueItemDelivered(ticket)); // Spoofing zombie

            hub.Caller.ZombieName = "zombie"; // Correct Zombie
            user.Zombies[0].ConnectionId = "another_connection_id"; // Wrong Connection-ID
            AssertionHelper.Throws<AuthenticationException>(() => hub.QueueItemDelivered(ticket)); // Wrong Connection-ID
        }

        [TestMethod]
        public void ShouldBeAbleToCleanUpAfterZombieDisconnect()
        {
            var hub = GetTestableZombieHub();
            hub.Caller.ZombieName = "zombie";
            hub.Caller.BelongsToUser = "username";

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

            hub.Disconnect();

            hub.MockedUserRepository.Verify(x => x.GetByUserName(It.Is<string>(s => s == "username")), Times.Once());
            hub.MockedUserRepository.Verify(r => r.Update(It.Is<ControllUser>(x => x.Zombies[0].ConnectionId == null)), Times.Once());
        }

        [TestMethod]
        public void ShouldBeAbleToRegisterAsZombie()
        {
            var hub = GetTestableZombieHub();

            hub.Caller.BelongsToUser = "Erik";
            hub.Caller.ZombieName = "ZombieName";

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

            hub.Caller.ZombieName = "existing_zombie";
            hub.Caller.BelongsToUser = "wrong_username";
            AssertionHelper.Throws<AuthenticationException>(() => hub.RegisterAsZombie("password")); // Username wrong but correct password
            AssertionHelper.Throws<AuthenticationException>(() => hub.RegisterAsZombie("wrong_password")); // Username wrong and incorrect password

            hub.Caller.BelongsToUser = "username";
            AssertionHelper.Throws<AuthenticationException>(() => hub.RegisterAsZombie("wrong_password")); // Username correct but incorrect password
            AssertionHelper.Throws<ArgumentException>(() => hub.RegisterAsZombie("password"));             // Username correct and correct password but zombie name already existing
        }

        private TestableZombieHub GetTestableZombieHub()
        {
            var mockedActivityService = new Mock<IActivityService>();
            var mockedUserRepository = new Mock<IControllUserRepository>();
            var mockedActivityRepository = new Mock<IGenericRepository<Activity>>();
            var mockedMessageQueueService = new Mock<IMessageQueueService>();

            var hub = new TestableZombieHub(mockedUserRepository, mockedActivityService, mockedActivityRepository, mockedMessageQueueService)
                {
                    Caller = new StatefulSignalAgent(new Mock<IConnection>().Object, "connid", "ZombieHub", new TrackingDictionary()),
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

            public TestableZombieHub(
                Mock<IControllUserRepository> mockedUserRepository, 
                Mock<IActivityService> mockedActivityService, 
                Mock<IGenericRepository<Activity>> mockedActivityRepository,
                Mock<IMessageQueueService> mockedMessageQueueService)
                : base(mockedUserRepository.Object, mockedActivityService.Object, mockedActivityRepository.Object, mockedMessageQueueService.Object)
            {
                MockedUserRepository = mockedUserRepository;
                MockedActivityService = mockedActivityService;
                MockedActivityRepository = mockedActivityRepository;
                MockedMessageQueueService = mockedMessageQueueService;
            }
        }
    }
}

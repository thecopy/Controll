using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using Controll.Common.ViewModels;
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

            bool result = hub.LogOn("username", "password", "zombie");
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

            Assert.IsFalse(hub.LogOn("wrong_username", "password", "zombie")); // Username wrong but correct password
            Assert.IsFalse(hub.LogOn("wrong_username", "wrong_password", "zombie")); // Username wrong and incorrect password

            Assert.IsFalse(hub.LogOn("username", "wrong_password", "zombie")); // Username correct but incorrect password
            Assert.IsFalse(hub.LogOn("username", "password", "wrong_zombie"));       // Username correct and correct password but zombie name wrong
        }

        [TestMethod]
        public void ShouldBeAbleToRespondWithArbritraryActivityResult()
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
            hub.MockedMessageQueueService.Setup(x => x.InsertActivityResult(It.Is<Guid>(g => g.Equals(ticket)), It.Is<ActivityCommandViewModel>(vm => vm.Label == "RESULT COMMAND"))).Verifiable();

            hub.LogOn("username", "password", "zombie");
            hub.ActivityResult(ticket, new ActivityCommandViewModel{Label = "RESULT COMMAND"});

            hub.MockedMessageQueueService.Verify(x => x.InsertActivityResult(It.Is<Guid>(g => g.Equals(ticket)), It.Is<ActivityCommandViewModel>(vm => vm.Label == "RESULT COMMAND")), Times.Once());
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

            hub.LogOn("username", "password", "zombie");
            hub.QueueItemDelivered(ticket);

            hub.MockedMessageQueueService.Verify(x => x.MarkQueueItemAsDelivered(It.Is<Guid>(guid => guid.Equals(ticket))));
        }



        [TestMethod]
        public void ShouldBeAbleToAddWhenSynchronizingActivityList()
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
                                    Activities = new List<Activity>(),
                                    ConnectionId = hub.Context.ConnectionId
                                }
                        }
            };

            var activityKey = Guid.NewGuid();

            var activities = new List<ActivityViewModel>
                {
                    new ActivityViewModel
                        {
                            Name = "TestActivity",
                            Key = activityKey,
                            Commands = new List<ActivityCommandViewModel>()
                        }
                };

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);

            hub.MockedUserRepository
                .Setup(x => x.Update(It.IsAny<ControllUser>()))
                .Callback((ControllUser u) =>
                    {
                        Assert.AreEqual(1, u.Zombies[0].Activities.Count);
                        Assert.AreEqual(activityKey, u.Zombies[0].Activities[0].Id);
                    });

            hub.SynchronizeActivities(activities);
        }

        [TestMethod]
        public void ShouldBeAbleToRemoveWhenSynchronizingActivityList()
        {
            var hub = GetTestableZombieHub();

            hub.Clients.Caller.ZombieName = "zombie";
            hub.Clients.Caller.BelongsToUser = "username";

            var activityKey = Guid.NewGuid();
            var activityKey2 = Guid.NewGuid();
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
                                    Activities = new List<Activity>
                                        {
                                            new Activity
                                                {
                                                    Id = activityKey,
                                                    Name = "TestActivity",
                                                    Commands = new List<ActivityCommand>()
                                                },
                                            new Activity
                                                {
                                                    Id = activityKey2,
                                                    Name = "TestActivity2",
                                                    Commands = new List<ActivityCommand>()
                                                }
                                        },
                                    ConnectionId = hub.Context.ConnectionId
                                }
                        }
            };

            var activities = new List<ActivityViewModel>
                {
                    new ActivityViewModel
                        {
                            Name = "TestActivity2",
                            Key = activityKey2,
                            Commands = new List<ActivityCommandViewModel>()
                        }
                };

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);

            hub.MockedUserRepository
                .Setup(x => x.Update(It.IsAny<ControllUser>()))
                .Callback((ControllUser u) =>
                    {
                        Assert.AreEqual(1, u.Zombies[0].Activities.Count);
                        Assert.AreEqual(activityKey2, u.Zombies[0].Activities[0].Id);
                    });

            hub.SynchronizeActivities(activities);
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


            Assert.IsFalse(hub.QueueItemDelivered(ticket)); // Not Logged On
            hub.Clients.Caller.BelongsToUser = "username";
            hub.Clients.Caller.ZombieName = "zombie";
            hub.LogOn("username", "password", "zombie");

            hub.Clients.Caller.BelongsToUser = "someone_else"; // Spoofing username
            Assert.IsFalse(hub.QueueItemDelivered(ticket)); // Spoofing username

            hub.Clients.Caller.BelongsToUser = "username"; // Correct Username
            hub.Clients.Caller.ZombieName = "another_zombie"; // Spoofing zombie
            Assert.IsFalse(hub.QueueItemDelivered(ticket)); // Spoofing zombie

            hub.Clients.Caller.ZombieName = "zombie"; // Correct Zombie
            user.Zombies[0].ConnectionId = "another_connection_id"; // Wrong Connection-ID
            Assert.IsFalse(hub.QueueItemDelivered(ticket)); // Wrong Connection-ID
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

            var result = hub.RegisterAsZombie("Erik", "password", "ZombieName");

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

            Assert.IsFalse(hub.RegisterAsZombie("wrong_username", "password", "zombie")); // Username wrong but correct password
            Assert.IsFalse(hub.RegisterAsZombie("wrong_username", "wrong_password", "zombie")); // Username wrong and incorrect password

            Assert.IsFalse(hub.RegisterAsZombie("username", "wrong_password", "zombie")); // Username correct but incorrect password
            Assert.IsFalse(hub.RegisterAsZombie("username", "wrong_password", "existing_zombie"));    // Username correct and correct password but zombie name already existing
        }

        private TestableZombieHub GetTestableZombieHub()
        {
            var mockedActivityService = new Mock<IActivityMessageLogService>();
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
            public Mock<IActivityMessageLogService> MockedActivityService { get; set; }
            public Mock<IGenericRepository<Activity>> MockedActivityRepository { get; set; }
            public Mock<IMessageQueueService> MockedMessageQueueService { get; set; }
            public Mock<ISession> MockedSession { get; set; }

            public TestableZombieHub(
                Mock<IControllUserRepository> mockedUserRepository, 
                Mock<IActivityMessageLogService> mockedActivityService, 
                Mock<IGenericRepository<Activity>> mockedActivityRepository,
                Mock<IMessageQueueService> mockedMessageQueueService,
                Mock<ISession> mockedSession)
                : base(mockedUserRepository.Object, mockedMessageQueueService.Object, mockedActivityService.Object, mockedSession.Object)
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

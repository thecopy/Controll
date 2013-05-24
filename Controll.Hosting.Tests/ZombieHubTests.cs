using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using Controll.Common.ViewModels;
using Controll.Hosting.Hubs;
using Controll.Hosting.Infrastructure;
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
        private IPrincipal GetPrincial(int userId, int zombieId)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ControllClaimTypes.UserIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
                    new Claim(ControllClaimTypes.ZombieIdentifier, zombieId.ToString(CultureInfo.InvariantCulture)),
                }, Constants.ControllAuthType));
        }

        [TestMethod]
        public void ShouldBeAbleToSignIn()
        {
            var zombie = new Zombie
            {
                Name = "zombie",
                Id = 1
            };

            var hub = GetTestableZombieHub();
            hub.MockedRequest.SetupGet(x => x.User).Returns(GetPrincial(1, 1));
            hub.MockedSession.Setup(x => x.Get<Zombie>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => zombie);

            hub.MockedSession.Setup(x => x.Update(It.Is<Zombie>(z => z == zombie && z.ConnectedClients.Any(cc => cc.ConnectionId == "conn-id")))).Verifiable();
            
            hub.SignIn();

            // Verify that connection-id was set
            hub.MockedSession.Verify(x => x.Update(It.Is<Zombie>(z => z == zombie && z.ConnectedClients.Any(cc => cc.ConnectionId == "conn-id"))), Times.Once());
        }
        
        [TestMethod]
        public void ShouldBeAbleToRespondWithArbritraryActivityResult()
        {
            var zombie = new Zombie
            {
                Name = "zombie",
                Id = 1
            };

            var hub = GetTestableZombieHub();
            hub.MockedRequest.SetupGet(x => x.User).Returns(GetPrincial(1, 1));
            hub.MockedSession.Setup(x => x.Get<Zombie>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => zombie);

            var user = new ControllUser
            {
                UserName = "username",
                Password = "password",
                Zombies = new List<Zombie>
                        {
                            zombie
                        }
            };

            var ticket = Guid.NewGuid();

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);
            hub.MockedMessageQueueService.Setup(x => x.InsertActivityResult(It.Is<Guid>(g => g.Equals(ticket)), It.Is<ActivityCommand>(vm => vm.Label == "RESULT COMMAND"))).Verifiable();

            hub.SignIn();
            hub.ActivityResult(ticket, new ActivityCommandViewModel{Label = "RESULT COMMAND"});

            hub.MockedMessageQueueService.Verify(x => x.InsertActivityResult(It.Is<Guid>(g => g.Equals(ticket)), It.Is<ActivityCommand>(vm => vm.Label == "RESULT COMMAND")), Times.Once());
        }

        [TestMethod]
        public void ShouldBeAbleToSetMessageAsDelivered()
        {
            var zombie = new Zombie
            {
                Name = "zombie",
                Id = 1
            };

            var hub = GetTestableZombieHub();
            hub.MockedRequest.SetupGet(x => x.User).Returns(GetPrincial(1, 1));
            hub.MockedSession.Setup(x => x.Get<Zombie>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => zombie);

            var user = new ControllUser
            {
                UserName = "username",
                Password = "password",
                Zombies = new List<Zombie>{zombie}
            };

            var ticket = Guid.NewGuid();

            hub.MockedUserRepository.Setup(x => x.GetByUserName("username")).Returns(user);
            hub.MockedMessageQueueService.Setup(x => x.MarkQueueItemAsDelivered(It.Is<Guid>(guid => guid.Equals(ticket)))).Verifiable();

            hub.SignIn();
            hub.QueueItemDelivered(ticket);

            hub.MockedMessageQueueService.Verify(x => x.MarkQueueItemAsDelivered(It.Is<Guid>(guid => guid.Equals(ticket))));
        }



        [TestMethod]
        public void ShouldBeAbleToAddWhenSynchronizingActivityList()
        {
            var zombie = new Zombie
            {
                Name = "zombie",
                Id = 1,
                Activities = new List<Activity>()
            };
            var user = new ControllUser
            {
                UserName = "username",
                Password = "password",
                Zombies = new List<Zombie> { zombie }
            };
            zombie.Owner = user;

            var hub = GetTestableZombieHub();
            hub.MockedRequest.SetupGet(x => x.User).Returns(GetPrincial(1, 1));
            hub.MockedSession.Setup(x => x.Get<Zombie>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => zombie);
            
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
            
            hub.MockedSession
                .Setup(x => x.Update(It.Is<Zombie>(z => z.Activities.Count == 1 && z.Activities[0].Id.Equals(activityKey))))
                .Verifiable();

            hub.SynchronizeActivities(activities);


            hub.MockedSession.Verify(x => x.Update(It.Is<Zombie>(z => z.Activities.Count == 1 && z.Activities[0].Id.Equals(activityKey))), Times.Once());
        }

        [TestMethod]
        public void ShouldBeAbleToRemoveWhenSynchronizingActivityList()
        {
            var activityKey = Guid.NewGuid();
            var activityKey2 = Guid.NewGuid();

            var zombie = new Zombie
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
                                        }
            };
            var user = new ControllUser
            {
                UserName = "username",
                Password = "password",
                Zombies = new List<Zombie> { zombie }
            };
            zombie.Owner = user;

            var hub = GetTestableZombieHub();
            hub.MockedRequest.SetupGet(x => x.User).Returns(GetPrincial(1, 1));
            hub.MockedSession.Setup(x => x.Get<Zombie>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => zombie);

            var activities = new List<ActivityViewModel>
                {
                    new ActivityViewModel
                        {
                            Name = "TestActivity2",
                            Key = activityKey2,
                            Commands = new List<ActivityCommandViewModel>()
                        }
                };


            hub.MockedSession
                           .Setup(x => x.Update(It.Is<Zombie>(z => z.Activities.Count == 1 && z.Activities[0].Id.Equals(activityKey2))))
                           .Verifiable();

            hub.SynchronizeActivities(activities);

            hub.MockedSession.Verify(x => x.Update(It.Is<Zombie>(z => z.Activities.Count == 1 && z.Activities[0].Id.Equals(activityKey2))), Times.Exactly(1));
        }
        
        [TestMethod]
        public void ShouldBeAbleToCleanUpAfterZombieDisconnect()
        {
            var zombie = new Zombie
            {
                Name = "zombie",
                Id = 1
            };

            var hub = GetTestableZombieHub();
            hub.MockedRequest.SetupGet(x => x.User).Returns(GetPrincial(1, 1));
            hub.MockedSession.Setup(x => x.Get<Zombie>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => zombie);
            
            zombie.ConnectedClients.Add(new ControllClient { ConnectionId = hub.Context.ConnectionId }); // Have something to clean up

            hub.MockedSession.Setup(r => r.Update(It.Is<Zombie>(x => x.ConnectedClients.Count == 0)))
                .Verifiable();

            hub.OnDisconnected();

            hub.MockedSession.Verify(r => r.Update(It.Is<Zombie>(x => x.ConnectedClients.Count == 0)), Times.Once());
        }

        [TestMethod]
        public void ShouldBeAbleToSignOut()
        {
            var zombie = new Zombie
            {
                Name = "zombie",
                Id = 1
            };

            var hub = GetTestableZombieHub();
            hub.MockedRequest.SetupGet(x => x.User).Returns(GetPrincial(1, 1));
            hub.MockedSession.Setup(x => x.Get<Zombie>(It.Is<Int32>(i => i == 1))).Returns((Int32 id) => zombie);
            zombie.ConnectedClients.Add(new ControllClient { ConnectionId = hub.Context.ConnectionId }); // Have something to clean up

            hub.MockedSession.Setup(r => r.Update(It.Is<Zombie>(x => x.ConnectedClients.Count == 0)));

            hub.SignOut();
            
            hub.MockedSession.Verify(r => r.Update(It.Is<Zombie>(x => x.ConnectedClients.Count == 0)), Times.Once());
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
                    Clients = new HubConnectionContext(mockPipeline.Object, mockedConnectionObject.Object, "ZombieHub", "conn-id", new StateChangeTracker())
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
            public Mock<IRequest> MockedRequest { get; set; }

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

                MockedRequest = new Mock<IRequest>();
                Context = new HubCallerContext(MockedRequest.Object, "conn-id");
            }
        }
    }
}

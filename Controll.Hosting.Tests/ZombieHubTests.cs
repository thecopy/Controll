using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using NUnit.Framework;
using Moq;
using NHibernate;

namespace Controll.Hosting.Tests
{
    
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
            
        [Test]
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
        
        [Test]
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

            hub.MockedControllService.Setup(x => x.InsertActivityResult(It.Is<Guid>(g => g.Equals(ticket)), It.Is<ActivityCommand>(vm => vm.Label == "RESULT COMMAND"))).Verifiable();

            hub.SignIn();
            hub.ActivityResult(ticket, new ActivityCommandViewModel{Label = "RESULT COMMAND"});

            hub.MockedControllService.Verify(x => x.InsertActivityResult(It.Is<Guid>(g => g.Equals(ticket)), It.Is<ActivityCommand>(vm => vm.Label == "RESULT COMMAND")), Times.Once());
        }

        [Test]
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

            hub.MockedControllService.Setup(x => x.MarkQueueItemAsDelivered(It.Is<Guid>(guid => guid.Equals(ticket)))).Verifiable();

            hub.SignIn();
            hub.QueueItemDelivered(ticket);

            hub.MockedControllService.Verify(x => x.MarkQueueItemAsDelivered(It.Is<Guid>(guid => guid.Equals(ticket))));
        }



        [Test]
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

        [Test]
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
        
        private TestableZombieHub GetTestableZombieHub()
        {
            var mockedControllRepository = new Mock<IControllRepository>();
            var mockedControllService = new Mock<IControllService>();
            var mockPipeline = new Mock<IHubPipelineInvoker>();
            var mockedConnectionObject = new Mock<IConnection>();
            var mockedSession = new Mock<ISession>();
            mockedSession.Setup(s => s.BeginTransaction()).Returns(new Mock<ITransaction>().Object);

            var hub = new TestableZombieHub(mockedControllRepository, mockedControllService, mockedSession)
                {
                    Clients = new HubConnectionContext(mockPipeline.Object, mockedConnectionObject.Object, "ZombieHub", "conn-id", new StateChangeTracker())
                };
            
            return hub;
        }

        private class TestableZombieHub : ZombieHub
        {
            public Mock<IControllRepository> MockedControllRepository { get; private set; }
            public Mock<IControllService> MockedControllService { get; set; }
            public Mock<ISession> MockedSession { get; private set; }
            public Mock<IRequest> MockedRequest { get; private set; }

            public TestableZombieHub(
                Mock<IControllRepository> mockedUserRepository, 
                Mock<IControllService> mockedControllService,
                Mock<ISession> mockedSession)
                : base(mockedUserRepository.Object, mockedControllService.Object, mockedSession.Object)
            {
                MockedControllRepository = mockedUserRepository;
                MockedControllService = mockedControllService;
                MockedSession = mockedSession;

                MockedRequest = new Mock<IRequest>();
                Context = new HubCallerContext(MockedRequest.Object, "conn-id");
            }
        }
    }
}

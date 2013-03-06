using System;
using System.Collections.Generic;
using System.Linq;
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
        public void ShouldBeAbleToAddActivityMessage()
        {
            var hub = GetTestableZombieHub();

            var activityTicket = Guid.NewGuid();

            hub.MockedActivityService.Setup(x => x.InsertActivityLogMessage(activityTicket, ActivityMessageType.Notification, "message")).Verifiable();

            hub.ActivityMessage(activityTicket, ActivityMessageType.Notification, "message");

            hub.MockedActivityService.Verify(x => x.InsertActivityLogMessage(activityTicket, ActivityMessageType.Notification, "message"), Times.Once());
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
                    Zombies = new List<Zombie>()
                };

            hub.MockedUserRepository.Setup(x => x.GetByUserName("Erik")).Returns(user);
            
            hub.MockedUserRepository.Setup(x => x.Update(It.Is<ControllUser>(u => u.Zombies.Any(z => z.Name == "ZombieName")))).Verifiable();

            var result = hub.RegisterAsZombie();

            hub.MockedUserRepository.Verify(x => x.Update(It.Is<ControllUser>(u => u.Zombies.Any(z => z.Name == "ZombieName"))));

            Assert.IsTrue(result);
        }

<<<<<<< HEAD
=======
        [TestMethod]
        public void ShouldBeAbleTo()
        {
            var hub = GetTestableZombieHub();

        }

>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
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

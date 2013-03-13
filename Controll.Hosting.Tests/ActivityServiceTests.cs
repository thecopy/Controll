using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class ActivityServiceTests
    {
        private static readonly Mock<IGenericRepository<ActivityInvocationQueueItem>> MockedInvocationQueueItemRepostiory = new Mock<IGenericRepository<ActivityInvocationQueueItem>>();
        private static readonly Mock<IGenericRepository<Activity>> MockedActivityRepository = new Mock<IGenericRepository<Activity>>();
        private static readonly Mock<IGenericRepository<Zombie>> MockedZombieRepository = new Mock<IGenericRepository<Zombie>>();

        [TestMethod]
        public void ShouldBeAbleToAddActivityToZombie()
        {
            var activityService = new ActivityService(
                MockedInvocationQueueItemRepostiory.Object,
                MockedActivityRepository.Object,
                MockedZombieRepository.Object);

            var activity = new Activity
                {
                    Name = "activityname",
                    Id = Guid.NewGuid()
                };
            var zombie = new Zombie
                {
                    Name = "zombiename",
                    Activities = new List<Activity>()
                };
            var user = new ControllUser
                {
                    Zombies = new List<Zombie> {zombie}
                };

            MockedZombieRepository.Setup(x => x.Update(It.Is<Zombie>(z => z.Name == "zombiename" && z.Activities.Any(a => a.Id == activity.Id)))).Verifiable();
            MockedActivityRepository.Setup(x => x.Get(It.Is<Guid>(g => g == activity.Id))).Returns(activity);

            activityService.AddActivityToZombie(zombie.Name, user, activity.Id);

            MockedZombieRepository.Verify(x => x.Update(It.Is<Zombie>(z => z.Name == "zombiename" && z.Activities.Any(a => a.Id == activity.Id))));
        }

        [TestMethod]
        public void ShouldBeAbleToInsertActivityLogMessage()
        {
            var activityService = new ActivityService(
                MockedInvocationQueueItemRepostiory.Object,
                MockedActivityRepository.Object,
                MockedZombieRepository.Object);

            var wh = new ManualResetEvent(false);
            Guid eventTicket = Guid.Empty;
            ActivityInvocationLogMessage logMessage = null;

            activityService.NewActivityLogItem += (sender, tuple) =>
                {
                    eventTicket = tuple.Item1;
                    logMessage = tuple.Item2;
                    wh.Set();
                };

            var invocationTicket = Guid.NewGuid();
            MockedInvocationQueueItemRepostiory.Setup(x => x.Get(invocationTicket)).Returns(new ActivityInvocationQueueItem {MessageLog = new List<ActivityInvocationLogMessage>()});


            MockedInvocationQueueItemRepostiory
                .Setup(x => x.Update(It.Is<ActivityInvocationQueueItem>(q =>
                                                                            q.MessageLog.Any(
                                                                                l =>
                                                                                    l.Message == "notification message" &&
                                                                                        l.Type == ActivityMessageType.Notification))))
                .Verifiable();

            activityService.InsertActivityLogMessage(invocationTicket, ActivityMessageType.Notification, "notification message");

            MockedInvocationQueueItemRepostiory
                .Verify(x => x.Update(It.Is<ActivityInvocationQueueItem>(
                    q => q.MessageLog.Any(l =>
                                              l.Message == "notification message" &&
                                                  l.Type == ActivityMessageType.Notification))));

            Assert.IsTrue(wh.WaitOne(1000));
            Assert.AreEqual(eventTicket, invocationTicket);
            Assert.AreEqual(logMessage.Message, "notification message");
        }

        [TestMethod]
        public void ShouldBeAbleToUpdateActivityResponse()
        {
            var activityService = new ActivityService(
                MockedInvocationQueueItemRepostiory.Object,
                MockedActivityRepository.Object,
                MockedZombieRepository.Object);

            var invocationTicket = Guid.NewGuid();

            MockedInvocationQueueItemRepostiory.Setup(x => x.Get(It.Is<Guid>(g => g == invocationTicket))).Returns(new ActivityInvocationQueueItem
                {
                    Activity = new Activity(),
                    Ticket = invocationTicket,
                    Parameters = new Dictionary<string, string>(),
                    MessageLog = new List<ActivityInvocationLogMessage>(),
                    RecievedAtCloud = DateTime.Now
                });

            MockedInvocationQueueItemRepostiory.Setup(x => x.Update(It.Is<ActivityInvocationQueueItem>(a => a.Response == "response"))).Verifiable();

            activityService.UpdateLogWithResponse(invocationTicket, "response");

            MockedInvocationQueueItemRepostiory.Verify(x => x.Update(It.Is<ActivityInvocationQueueItem>(a => a.Response == "response")));
        }

        [TestMethod]
        public void ShouldBeAbleToGetTheLatestStartedActivityForAZombie()
        {
            var invocationQueueItemRepository = new InMemoryRepository<ActivityInvocationQueueItem>();

            var activityService = new ActivityService(
                invocationQueueItemRepository,
                MockedActivityRepository.Object,
                MockedZombieRepository.Object);

            var invocationTicket = Guid.NewGuid();

            var activity = Builder<Activity>
                .CreateNew()
                .With(x => x.Id = Guid.NewGuid())
                .With(x => x.Commands = Builder<ActivityCommand>.CreateListOfSize(5).Random(1).With(a => a.Name = "command").Build())
                .Build();

            var zombie = Builder<Zombie>
                .CreateNew()
                .With(x => x.Activities = new List<Activity> {activity})
                .Build();

            var user = Builder<ControllUser>
                .CreateNew()
                .With(x => x.Zombies = new List<Zombie> {zombie})
                .Build();

            var invocation = new ActivityInvocationQueueItem
                {
                    Activity = activity,
                    Ticket = invocationTicket,
                    Parameters = new Dictionary<string, string>(),
                    MessageLog = new List<ActivityInvocationLogMessage>(),
                    RecievedAtCloud = DateTime.Now,
                    Reciever = zombie,
                    Delivered = DateTime.Now
                };
            var invocationOld = new ActivityInvocationQueueItem
                {
                    Activity = activity,
                    Ticket = Guid.NewGuid(),
                    Parameters = new Dictionary<string, string>(),
                    MessageLog = new List<ActivityInvocationLogMessage>(),
                    RecievedAtCloud = DateTime.Now.AddDays(-5),
                    Reciever = zombie,
                    Delivered = DateTime.Now.AddDays(-4)
                };
            var invocationAnotherOld = new ActivityInvocationQueueItem
                {
                    Activity = activity,
                    Ticket = Guid.NewGuid(),
                    Parameters = new Dictionary<string, string>(),
                    MessageLog = new List<ActivityInvocationLogMessage>(),
                    RecievedAtCloud = DateTime.Now.AddDays(-35),
                    Reciever = zombie,
                    Delivered = DateTime.Now.AddDays(-33)
                };

            invocationQueueItemRepository.Add(invocationAnotherOld);
            invocationQueueItemRepository.Add(invocation); // <-- This is the latest
            invocationQueueItemRepository.Add(invocationOld);

            var fetchedTicket = activityService.GetLatestStartedActivity(user, zombie, activity.Id);
            var fetchedQueueItem = invocationQueueItemRepository.Get(fetchedTicket);
            Assert.AreEqual(invocationTicket, fetchedTicket);
            Assert.AreEqual(invocation.Activity.Id, fetchedQueueItem.Activity.Id);
            Assert.AreEqual(invocation, fetchedQueueItem);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class MessageQueueServiceTests
    {
        private static readonly Mock<IGenericRepository<ActivityInvocationQueueItem>> MockedInvocationQueueItemRepostiory = new Mock<IGenericRepository<ActivityInvocationQueueItem>>();
        private static readonly Mock<IGenericRepository<ActivityDownloadOrderQueueItem>> MockedActivityDownloadQueueItemRepostiory = new Mock<IGenericRepository<ActivityDownloadOrderQueueItem>>();
        private static readonly Mock<IGenericRepository<QueueItem>> MockedQueueItemRepostiory = new Mock<IGenericRepository<QueueItem>>();

        [TestMethod]
        public void ShouldBeAbleToInsertActivityInvocationQueueItem()
        {
            var messageQueueService = new MessageQueueService(
                MockedInvocationQueueItemRepostiory.Object,
                MockedActivityDownloadQueueItemRepostiory.Object, 
                MockedQueueItemRepostiory.Object);

            var zombie = new Zombie
                {
                    Name = "zombiename"
                };
            var activity = new Activity
                {
                    Name = "activityname"
                };
            var paramsDictionary = new Dictionary<string, string>();
            const string commandName = "commandName";
            zombie.Activities = new [] {activity};

            var wh = new ManualResetEvent(false);

            MockedInvocationQueueItemRepostiory
                .Setup(x => x.Add(
                    It.Is<ActivityInvocationQueueItem>(
                        a =>
                            a.Activity.Name == "activityname" &&
                                a.Reciever.Name == "zombiename" &&
                                a.CommandName == commandName &&
                                a.Parameters.Count == 0 &&
                                a.Type == QueueItemType.ActivityInvocation
                        )))
                .Callback(() => wh.Set());

            messageQueueService.InsertActivityInvocation(zombie, activity, paramsDictionary, commandName);

            Assert.IsTrue(wh.WaitOne(1000));
        }

        [TestMethod]
        public void ShouldBeAbleToInsertActivityDownloadOrderQueueItem()
        {
            var messageQueueService = new MessageQueueService(
                MockedInvocationQueueItemRepostiory.Object,
                MockedActivityDownloadQueueItemRepostiory.Object,
                MockedQueueItemRepostiory.Object);

            var zombie = new Zombie
                {
                    Name = "zombiename"
                };
            var activity = new Activity
                {
                    Name = "activityname"
                };

            zombie.Activities = new[] { activity };

            var wh = new ManualResetEvent(false);

            MockedActivityDownloadQueueItemRepostiory
                .Setup(x => x.Add(
                    It.Is<ActivityDownloadOrderQueueItem>(
                        a =>
                            a.Activity.Name == "activityname" &&
                            a.Reciever.Name == "zombiename" &&
                                a.Type == QueueItemType.DownloadOrder
                        )))
                .Callback(() => wh.Set());

            messageQueueService.InsertActivityDownloadOrder(zombie, activity);

            Assert.IsTrue(wh.WaitOne(1000));
        }

        [TestMethod]
        public void ShouldBeAbleToMarkQueueItemAsDelivered()
        {
            var messageQueueService = new MessageQueueService(
                MockedInvocationQueueItemRepostiory.Object,
                MockedActivityDownloadQueueItemRepostiory.Object,
                MockedQueueItemRepostiory.Object);
            var ticket = Guid.NewGuid();

            var queueItem = new Mock<QueueItem>();
            queueItem.SetupAllProperties();
            queueItem.SetupGet(x => x.Ticket).Returns(ticket);

            MockedQueueItemRepostiory.Setup(x => x.Get(ticket)).Returns(queueItem.Object);
            MockedQueueItemRepostiory.Setup(x => x.Update(It.Is<QueueItem>(q => q.Ticket == ticket && q.Delivered.HasValue))).Verifiable();

            messageQueueService.MarkQueueItemAsDelivered(ticket);

            MockedQueueItemRepostiory.Verify(x => x.Update(It.Is<QueueItem>(q => q.Ticket == ticket && q.Delivered.HasValue)), Times.Once());
        }
    }
}

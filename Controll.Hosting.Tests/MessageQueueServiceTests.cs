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
        [TestMethod]
        public void ShouldBeAbleToInsertActivityInvocationQueueItem()
        {
            var mockedQueueItemRepostiory = new Mock<IGenericRepository<QueueItem>>();
            var messageQueueService = new MessageQueueService(
                mockedQueueItemRepostiory.Object);

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

            mockedQueueItemRepostiory
                .Setup(x => x.Add(
                    It.Is<ActivityInvocationQueueItem>(
                        a =>
                            a.Activity.Name == "activityname" &&
                                a.Reciever.Name == "zombiename" &&
                                a.CommandName == commandName &&
                                a.Parameters.Count == 0 &&
                                a.Type == QueueItemType.ActivityInvocation
                        )))
                .Callback((QueueItem qi) =>qi.Ticket = Guid.NewGuid())
                .Verifiable();

            var ticket = messageQueueService.InsertActivityInvocation(zombie, activity, paramsDictionary, commandName);
            Assert.AreNotEqual(Guid.Empty, ticket);

            mockedQueueItemRepostiory.Verify(x => x.Add(
                    It.Is<ActivityInvocationQueueItem>(
                        a =>
                            a.Activity.Name == "activityname" &&
                                a.Reciever.Name == "zombiename" &&
                                a.CommandName == commandName &&
                                a.Parameters.Count == 0 &&
                                a.Type == QueueItemType.ActivityInvocation
                        )), Times.Once());
        }

       [TestMethod]
        public void ShouldBeAbleToMarkQueueItemAsDelivered()
        {
            var mockedQueueItemRepostiory = new Mock<IGenericRepository<QueueItem>>();
            var messageQueueService = new MessageQueueService(
                mockedQueueItemRepostiory.Object);
            var ticket = Guid.NewGuid();

            var queueItem = new Mock<QueueItem>();
            queueItem.SetupAllProperties();
            queueItem.SetupGet(x => x.Ticket).Returns(ticket);

            mockedQueueItemRepostiory.Setup(x => x.Get(ticket)).Returns(queueItem.Object);
            mockedQueueItemRepostiory.Setup(x => x.Update(It.Is<QueueItem>(q => q.Ticket == ticket && q.Delivered.HasValue))).Verifiable();

            messageQueueService.MarkQueueItemAsDelivered(ticket);

            mockedQueueItemRepostiory.Verify(x => x.Update(It.Is<QueueItem>(q => q.Ticket == ticket && q.Delivered.HasValue)), Times.Once());
        }
    }
}

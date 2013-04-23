using System;
using System.Collections.Generic;
using System.Threading;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
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
            var mockedQueueItemRepostiory = new Mock<IQueueItemRepostiory>();
            var mockedConnectionManager = new Mock<IConnectionManager>();
            var messageQueueService = new MessageQueueService(
                mockedQueueItemRepostiory.Object,
                mockedConnectionManager.Object);

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
                                a.Parameters.Count == 0 &&
                                a.Type == QueueItemType.ActivityInvocation
                        )))
                .Callback((QueueItem qi) =>qi.Ticket = Guid.NewGuid())
                .Verifiable();

            var ticket = messageQueueService.InsertActivityInvocation(zombie, activity, paramsDictionary, "connectionId");
            Assert.AreNotEqual(Guid.Empty, ticket);

            mockedQueueItemRepostiory.Verify(x => x.Add(
                    It.Is<ActivityInvocationQueueItem>(
                        a =>
                            a.Activity.Name == "activityname" &&
                                a.Reciever.Name == "zombiename" &&
                                a.Parameters.Count == 0 &&
                                a.Type == QueueItemType.ActivityInvocation &&
                                a.SenderConnectionId == "connectionId"
                        )), Times.Once());
        }

        [TestMethod]
        public void ShouldBeAbleToMarkQueueItemAsDelivered()
        {
            var mockedQueueItemRepostiory = new Mock<IQueueItemRepostiory>();
            var mockedConnectionManager = new Mock<IConnectionManager>();
            var mockedHubContext = new Mock<IHubContext>();
            var mockedConnectionContext = new Mock<IHubConnectionContext>();

            var messageQueueService = new MessageQueueService(
                mockedQueueItemRepostiory.Object,
                mockedConnectionManager.Object);

            var ticket = Guid.NewGuid();

            var queueItem = new Mock<QueueItem>();
            queueItem.SetupAllProperties();
            queueItem.SetupGet(x => x.Ticket).Returns(ticket);
            queueItem.SetupGet(x => x.SenderConnectionId).Returns("Connid");

            // Setup the hubcontext and client objects
            mockedConnectionContext.Setup(x => x.Client(It.IsAny<String>())).Returns(new MockedClient());
            mockedHubContext.Setup(x => x.Clients).Returns(mockedConnectionContext.Object);
            mockedConnectionManager.Setup(x => x.GetHubContext<ClientHub>()).Returns(mockedHubContext.Object); 

            mockedQueueItemRepostiory.Setup(x => x.Get(ticket)).Returns(queueItem.Object);
            mockedQueueItemRepostiory.Setup(x => x.Update(It.Is<QueueItem>(q => q.Ticket == ticket && q.Delivered.HasValue))).Verifiable();

            messageQueueService.MarkQueueItemAsDelivered(ticket);

            mockedQueueItemRepostiory.Verify(x => x.Update(It.Is<QueueItem>(q => q.Ticket == ticket && q.Delivered.HasValue)), Times.Once());
        }

        public class MockedClient
        {
            public void MessageDelivered(Guid s)
            {
                // Good for you!
            }
        }
    }
}

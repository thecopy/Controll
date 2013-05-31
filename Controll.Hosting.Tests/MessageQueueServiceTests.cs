using System;
using System.Collections.Generic;
using System.Threading;
using Controll.Common.ViewModels;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using NUnit.Framework;
using Moq;
using NHibernate;

namespace Controll.Hosting.Tests
{
    
    public class MessageQueueServiceTests
    {
        [Test]
        public void ShouldBeAbleToInsertActivityInvocationQueueItem()
        {
            var mockedSession = new Mock<ISession>();
            var mockedConnectionManager = new Mock<IConnectionManager>();
            var mockedControllRepository = new Mock<IControllRepository>();

            var service = new ControllService(
                mockedSession.Object,
                mockedControllRepository.Object,
                mockedConnectionManager.Object);

            var user = new ControllUser
                {
                    UserName = "username"
                };

            var zombie = new Zombie
                {
                    Name = "zombiename",
                    Owner = user
                };
            var activity = new Activity
                {
                    Name = "activityname"
                };
            var paramsDictionary = new Dictionary<string, string>();
            const string commandName = "commandName";
            zombie.Activities = new [] {activity};

            mockedSession
                .Setup(x => x.Save(
                    It.Is<ActivityInvocationQueueItem>(
                        a =>
                        a.Activity.Name == "activityname" &&
                        a.Reciever.GetType() == typeof (Zombie) &&
                        ((Zombie) a.Reciever).Name == "zombiename" &&
                        a.Sender.GetType() == typeof (ControllUser) &&
                        ((ControllUser) a.Sender).UserName == "username" &&
                        a.CommandName == commandName &&
                        a.Parameters.Count == 0 &&
                        a.Type == QueueItemType.ActivityInvocation
                        )))
                .Verifiable();

            service.InsertActivityInvocation(zombie, activity, paramsDictionary, commandName, "connectionId");

            mockedSession.Verify(x => x.Save(
                    It.Is<ActivityInvocationQueueItem>(
                        a =>
                            a.Activity.Name == "activityname" &&
                                a.Reciever.GetType() == typeof(Zombie) &&
                                ((Zombie)a.Reciever).Name == "zombiename" &&
                                a.Sender.GetType() == typeof(ControllUser) &&
                                ((ControllUser)a.Sender).UserName == "username" &&
                                a.CommandName == commandName &&
                                a.Parameters.Count == 0 &&
                                a.Type == QueueItemType.ActivityInvocation &&
                                a.Sender == user
                        )), Times.Once());
        }
        [Test]
        public void ShouldBeAbleToInsertActivityResultQueueItem()
        {
            var mockedSession = new Mock<ISession>();
            var mockedConnectionManager = new Mock<IConnectionManager>();
            var mockedControllRepository = new Mock<IControllRepository>();

            var service = new ControllService(
                mockedSession.Object,
                mockedControllRepository.Object,
                mockedConnectionManager.Object);

            var user = new ControllUser
            {
                UserName = "username"
            };

            var zombie = new Zombie
            {
                Name = "zombiename",
                Owner = user
            };
            var activityCommand = new ActivityCommand
            {
                Name = "activityname",
                Label = "labellll"
            };
            var ticket = Guid.NewGuid();
            var communicator = new Mock<ClientCommunicator>();
            communicator.SetupGet(x => x.ConnectedClients).Returns(new List<ControllClient>());
            var invocationQueueItem = new ActivityInvocationQueueItem
                {
                    Ticket = ticket,
                    Reciever = communicator.Object,
                    Sender = communicator.Object
                };

            mockedSession.Setup(x => x.Get<ActivityInvocationQueueItem>(It.Is<Guid>(g => g.Equals(ticket))))
                .Returns(invocationQueueItem);

            mockedSession.Setup(q => q.Save(
                It.Is<ActivityResultQueueItem>(x =>
                                               x.ActivityCommand == activityCommand &&
                                               x.InvocationTicket == ticket))).Verifiable();

            service.InsertActivityResult(ticket, activityCommand);

            mockedSession.Verify(q => q.Save(
                It.Is<ActivityResultQueueItem>(x =>
                                               x.ActivityCommand == activityCommand &&
                                               x.InvocationTicket == ticket)), Times.Once());
        }

        [Test]
        public void ShouldBeAbleToMarkQueueItemAsDelivered()
        {
            var mockedConnectionManager = new Mock<IConnectionManager>();
            var mockedSession = new Mock<ISession>();
            var mockedControllRepository = new Mock<IControllRepository>();

            var mockedHubContext = new Mock<IHubContext>();
            var mockedConnectionContext = new Mock<IHubConnectionContext>();

            var service = new ControllService(
                mockedSession.Object,
                mockedControllRepository.Object,
                mockedConnectionManager.Object);


            var ticket = Guid.NewGuid();

            var queueItem = new Mock<QueueItem>();
            queueItem.SetupAllProperties();
            queueItem.SetupGet(x => x.Ticket).Returns(ticket);
            queueItem.SetupGet(x => x.Sender).Returns(() =>
                {
                    var controllUser = new ControllUser();
                    controllUser.ConnectedClients.Add(new ControllClient { ConnectionId = "Connid" });
                    return controllUser;
                });

            // Setup the hubcontext and client objects
            mockedConnectionContext.Setup(x => x.Client(It.IsAny<String>())).Returns(new MockedClient());
            mockedHubContext.Setup(x => x.Clients).Returns(mockedConnectionContext.Object);
            mockedConnectionManager.Setup(x => x.GetHubContext<ClientHub>()).Returns(mockedHubContext.Object); 

            mockedSession.Setup(x => x.Get<QueueItem>(ticket)).Returns(queueItem.Object);
            mockedSession.Setup(x => x.Update(It.Is<QueueItem>(q => q.Ticket == ticket && q.Delivered.HasValue))).Verifiable();

            service.MarkQueueItemAsDelivered(ticket);

            mockedSession.Verify(x => x.Update(It.Is<QueueItem>(q => q.Ticket == ticket && q.Delivered.HasValue)), Times.Once());
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

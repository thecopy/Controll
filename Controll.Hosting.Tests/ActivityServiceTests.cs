using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
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
        [TestMethod]
        public void ShouldBeAbleToGetActivityLogMessages()
        {
            var mockedInvocationQueueItemRepostiory = new Mock<IGenericRepository<ActivityInvocationQueueItem>>();
            var activityService = new ActivityMessageLogService(mockedInvocationQueueItemRepostiory.Object, null);

            var activity = new Activity
                {
                    Name = "activity",
                    Commands = new List<ActivityCommand>
                        {
                            new ActivityCommand
                                {
                                    Name = "commandName"
                                }
                        }
                };

            var zombie = new Zombie()
                {
                    Name = "zombieName",
                    Id = 123,
                    Activities = new List<Activity>{activity}
                };
            var deliveredDate = DateTime.Parse("2010-10-10");

            var logMessages = new List<ActivityInvocationLogMessage>
                {
                    new ActivityInvocationLogMessage
                        {
                            Date = deliveredDate.AddSeconds(10),
                            Message = "msg1",
                            Type = ActivityMessageType.Started
                        },
                    new ActivityInvocationLogMessage
                        {
                            Date = deliveredDate.AddSeconds(20),
                            Message = "msg2",
                            Type = ActivityMessageType.Failed
                        }
                };

            var queueItems = new List<ActivityInvocationQueueItem>()
                {
                    new ActivityInvocationQueueItem()
                        {
                            Activity = activity,
                            CommandName = activity.Commands[0].Name,
                            Delivered = deliveredDate,
                            Reciever = zombie,
                            Parameters = new Dictionary<string, string>
                                {
                                    {"param1", "value1"}
                                },
                            MessageLog = logMessages

                        }
                };

            mockedInvocationQueueItemRepostiory.SetupGet(x => x.Query).Returns(queueItems.AsQueryable);

            // Get activity log summary for each activity invocation, in this case only one.
            var result = activityService.GetActivityLog(zombie);

            Assert.AreEqual(queueItems.Count, result.Count);

            var gottenLogBook = result.ElementAt(0);

            Assert.AreEqual(activity.Name, gottenLogBook.ActivityName);
            Assert.AreEqual(activity.Commands.ElementAt(0).Label, gottenLogBook.CommandLabel);

            Assert.AreEqual(deliveredDate, gottenLogBook.Delivered);
            Assert.AreEqual(deliveredDate.AddSeconds(10), gottenLogBook.Started);
            Assert.AreEqual(deliveredDate.AddSeconds(20), gottenLogBook.Finished);
            
            for (int msgIndex = 0; msgIndex < logMessages.Count; msgIndex++)
            {
                var gottenMessage = gottenLogBook.Messages.ElementAt(msgIndex);
                var originalMessage = logMessages[msgIndex];

                Assert.AreEqual(originalMessage.Date, gottenMessage.Date);
                Assert.AreEqual(originalMessage.Message, gottenMessage.Message);
                Assert.AreEqual(originalMessage.Type, gottenMessage.MessageType);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToInsertActivityLogMessage()
        {
            var mockedInvocationQueueItemRepostiory = new Mock<IGenericRepository<ActivityInvocationQueueItem>>();
            var activityService = new ActivityMessageLogService(mockedInvocationQueueItemRepostiory.Object, null);

            var wh = new ManualResetEvent(false);


            var invocationTicket = Guid.NewGuid();
            mockedInvocationQueueItemRepostiory.Setup(x => x.Get(invocationTicket)).Returns(new ActivityInvocationQueueItem {MessageLog = new List<ActivityInvocationLogMessage>()});


            mockedInvocationQueueItemRepostiory
                .Setup(x => x.Update(It.Is<ActivityInvocationQueueItem>(q =>
                                                                            q.MessageLog.Any(
                                                                                l =>
                                                                                    l.Message == "notification message" &&
                                                                                        l.Type == ActivityMessageType.Notification))))
                .Verifiable();

            activityService.InsertActivityLogMessage(invocationTicket, ActivityMessageType.Notification, "notification message");

            mockedInvocationQueueItemRepostiory
                .Verify(x => x.Update(It.Is<ActivityInvocationQueueItem>(
                    q => q.MessageLog.Any(l =>
                                              l.Message == "notification message" &&
                                                  l.Type == ActivityMessageType.Notification))));
        }
        [TestMethod]
        public void ShouldBeAbleToGetUndeliveredIntermidiateCommandResults()
        {
            var mockedInvocationQueueItemRepostiory = new Mock<IGenericRepository<ActivityInvocationQueueItem>>();
            var mockedActivityResultQueueItemRepostiory = new Mock<IGenericRepository<ActivityResultQueueItem>>();
            var activityService = new ActivityMessageLogService(
                mockedInvocationQueueItemRepostiory.Object, 
                mockedActivityResultQueueItemRepostiory.Object);

            var activity = new Activity
                {
                    Name = "activity",
                    Commands = new List<ActivityCommand>
                        {
                            new ActivityCommand
                                {
                                    Name = "anotherCommand"
                                }
                        }
                };

            var command = new ActivityCommand
            {
                Name = "commandName",
                Id = Guid.NewGuid(),
                Label = "commandLabel",
                ParameterDescriptors = new List<ParameterDescriptor>()
            };

            var zombie = new Zombie()
            {
                Name = "zombieName",
                Id = 123
            };

            var invocationTicket = Guid.NewGuid();
            var resultTicket = Guid.NewGuid();
            var queueItems = new List<ActivityResultQueueItem>()
                {
                    new ActivityResultQueueItem()
                        {
                            ActivityCommand = command,
                            Ticket = resultTicket,
                            InvocationTicket = invocationTicket,
                            Sender = zombie,
                            RecievedAtCloud = DateTime.Parse("2010-10-10"),
                        }
                };

            var invocationQueueItems = new List<ActivityInvocationQueueItem>
                {
                    new ActivityInvocationQueueItem
                        {
                            Ticket = invocationTicket,
                            Activity = activity
                        }
                };

            mockedInvocationQueueItemRepostiory.SetupGet(x => x.Query).Returns(invocationQueueItems.AsQueryable);
            mockedActivityResultQueueItemRepostiory.SetupGet(x => x.Query).Returns(queueItems.AsQueryable);

            var results = activityService.GetUndeliveredIntermidiates(zombie);

            Assert.AreEqual(queueItems.Count, results.Count);
            Assert.AreEqual(resultTicket, results[0].ResultTicket);

            var mockedItem = queueItems.ElementAt(0).ActivityCommand;
            var resultItem = results.ElementAt(0);
            
            Assert.AreEqual(mockedItem.Name, resultItem.Name);
            Assert.AreEqual(mockedItem.Label, resultItem.Label);
            Assert.AreEqual(activity.Name, resultItem.Activity.Name);
        }
      /*
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
        }*/
    }
}

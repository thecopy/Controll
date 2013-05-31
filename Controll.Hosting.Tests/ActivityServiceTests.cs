using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using FizzWare.NBuilder;
using HibernatingRhinos.Profiler.Appender.NHibernate;
using NUnit.Framework;
using Moq;
using NHibernate;
using NHibernate.Linq;

namespace Controll.Hosting.Tests
{
    
    public class ActivityServiceTests : TestBase
    {
        [Test]
        public void ShouldBeAbleToGetActivityLogMessages()
        {
            
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var activityService = new ControllService(session, null, null);

                var user = new ControllUser()
                    {
                        UserName = "name",
                        Email = "mail",
                        Password = "pass"
                    };

                var activity = new Activity
                    {
                        Id = Guid.NewGuid(),
                        LastUpdated = DateTime.Now,
                        Name = "activity",
                        Commands = new List<ActivityCommand>
                            {
                                new ActivityCommand
                                    {
                                        Name = "commandName"
                                    }
                            }
                    };

                var zombie = new Zombie
                    {
                        Name = "zombieName",
                        Owner = user,
                        Activities = new List<Activity> {activity}
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

                var queueItem = new ActivityInvocationQueueItem()
                            {
                                Activity = activity,
                                CommandName = activity.Commands[0].Name,
                                Delivered = deliveredDate,
                                RecievedAtCloud = DateTime.Now,
                                Reciever = zombie,
                                Sender = user,
                                Parameters = new Dictionary<string, string>
                                    {
                                        {"param1", "value1"}
                                    },
                                MessageLog = logMessages

                            };

                session.Save(user);
                session.Save(zombie);
                session.Save(activity);
                session.Save(queueItem);

                // Get activity log summary for each activity invocation, in this case only one.
                var result = activityService.GetActivityLog(zombie);

                Assert.AreEqual(1, result.Count);

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
        }

        [Test]
        public void ShouldBeAbleToInsertActivityLogMessage()
        {
            var mockedSession = new Mock<ISession>();
            var activityService = new ControllService(mockedSession.Object, null, null);


            var invocationTicket = Guid.NewGuid();
            mockedSession.Setup(x => x.Get<ActivityInvocationQueueItem>(invocationTicket)).Returns(new ActivityInvocationQueueItem { MessageLog = new List<ActivityInvocationLogMessage>() });


            mockedSession
                .Setup(x => x.Update(It.Is<ActivityInvocationQueueItem>(q =>
                                                                            q.MessageLog.Any(
                                                                                l =>
                                                                                    l.Message == "notification message" &&
                                                                                        l.Type == ActivityMessageType.Notification))))
                .Verifiable();

            activityService.InsertActivityLogMessage(invocationTicket, ActivityMessageType.Notification, "notification message");

            mockedSession
                .Verify(x => x.Update(It.Is<ActivityInvocationQueueItem>(
                    q => q.MessageLog.Any(l =>
                                              l.Message == "notification message" &&
                                                  l.Type == ActivityMessageType.Notification))));
        }

        [Test]
        public void ShouldBeAbleToGetUndeliveredIntermidiateCommandResults()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var activityService = new ControllService(session, null, null);

                var activity = new Activity
                    {
                        Id = Guid.NewGuid(),
                        LastUpdated = DateTime.Now,
                        Name = "activity",
                        Commands = new List<ActivityCommand>
                            {
                                new ActivityCommand
                                    {
                                        Name = "anotherCommand"
                                    }
                            }
                    };

                var user = new ControllUser()
                    {
                        UserName = "name",
                        Email = "mail",
                        Password = "pass"
                    };

                var command = new ActivityCommand
                    {
                        Name = "commandName",
                        Label = "commandLabel",
                        ParameterDescriptors = new List<ParameterDescriptor>()
                    };

                var zombie = new Zombie()
                    {
                        Name = "zombieName",
                        Owner = user
                    };

                var activityResultQueueItem = new ActivityResultQueueItem()
                            {
                                ActivityCommand = command,
                                Sender = zombie,
                                Reciever = user,
                                Activity = activity,
                                RecievedAtCloud = DateTime.Now
                            };

                var invocationQueueItem = new ActivityInvocationQueueItem
                            {
                                Activity = activity,
                                Sender = user,
                                Reciever = zombie,
                                RecievedAtCloud = DateTime.Now
                            };

                session.Save(user);
                session.Save(zombie);
                session.Save(activity);
                session.Save(invocationQueueItem);

                activityResultQueueItem.InvocationTicket = invocationQueueItem.Ticket;
                session.Save(activityResultQueueItem);

                var results = activityService.GetUndeliveredIntermidiates(zombie);

                Assert.AreEqual(1, results.Count);

                var mockedItem = activityResultQueueItem.ActivityCommand;
                var resultItem = results.ElementAt(0);

                Assert.AreEqual(invocationQueueItem.Ticket, resultItem.ResultTicket);
                Assert.AreEqual(mockedItem.Name, resultItem.Name);
                Assert.AreEqual(mockedItem.Label, resultItem.Label);
                Assert.AreEqual(activity.Name, resultItem.Activity.Name);
            }
        }

    }
}

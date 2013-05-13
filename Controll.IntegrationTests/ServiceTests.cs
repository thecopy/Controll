using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting;
using Controll.Hosting.Helpers;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NHibernate;

namespace Controll.IntegrationTests
{
    [TestClass]
    public class ServiceTests : TestBase
    {
        [TestMethod]
        public void ShouldBeAbleToGetUndeliveredIntermidiateCommandResults()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var invocationQueueItemRepostiory = new GenericRepository<ActivityInvocationQueueItem>(session);
                var activityResultQueueItemRepostiory = new GenericRepository<ActivityResultQueueItem>(session);
                var userRepository = new ControllUserRepository(session);
                var zombieRepository = new GenericRepository<Zombie>(session);
                var activityRepostiory = new GenericRepository<Activity>(session);

                var activityService = new ActivityMessageLogService(
                    invocationQueueItemRepostiory,
                    activityResultQueueItemRepostiory);

                var user = new ControllUser
                    {
                        UserName = "username123",
                        Email = "mail123",
                        Password = "password"
                    };


                var activity = new Activity
                    {
                        Name = "activity",
                        LastUpdated = DateTime.Parse("2010-10-10"),
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
                        RecievedAtCloud = DateTime.Parse("2010-10-10")
                    };

                var invocationQueueItem = new ActivityInvocationQueueItem
                    {
                        Activity = activity,
                        Reciever = zombie,
                        Sender = user,
                        RecievedAtCloud = DateTime.Parse("2010-10-10")
                    };


                activityRepostiory.Add(activity);
                userRepository.Add(user);
                zombieRepository.Add(zombie);
                invocationQueueItemRepostiory.Add(invocationQueueItem);
                activityResultQueueItem.InvocationTicket = invocationQueueItem.Ticket;

                activityResultQueueItemRepostiory.Add(activityResultQueueItem);

                var results = activityService.GetUndeliveredIntermidiates(zombie);

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(activityResultQueueItem.Ticket, results[0].ResultTicket);
                var resultItem = results.ElementAt(0);

                Assert.AreEqual(activityResultQueueItem.ActivityCommand.Name, resultItem.Name);
                Assert.AreEqual(activityResultQueueItem.ActivityCommand.Label, resultItem.Label);
                Assert.AreEqual(activity.Name, resultItem.Activity.Name);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.IntegrationTests
{
    [TestClass]
    public class ServiceTests : TestBase
    {
        [TestMethod]
        public void ShouldBeAbleToGetUndeliveredIntermidiateCommandResults()
        {
            using(var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var mockedInvocationQueueItemRepostiory = new GenericRepository<ActivityInvocationQueueItem>(session);
                var mockedActivityResultQueueItemRepostiory = new GenericRepository<ActivityResultQueueItem>(session);
                var userRepository = new ControllUserRepository(session);
                var zombieRepository = new GenericRepository<Zombie>(session);
                var activityRepostiory = new GenericRepository<Activity>(session);

                var activityService = new ActivityMessageLogService(
                    mockedInvocationQueueItemRepostiory,
                    mockedActivityResultQueueItemRepostiory);

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

                var activityResultQueueItem =  new ActivityResultQueueItem()
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
                mockedInvocationQueueItemRepostiory.Add(invocationQueueItem);
                activityResultQueueItem.InvocationTicket = invocationQueueItem.Ticket;

                mockedActivityResultQueueItemRepostiory.Add(activityResultQueueItem);

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

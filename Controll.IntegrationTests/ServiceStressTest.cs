using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Controll.Common.ViewModels;
using Controll.Hosting;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using log4net.Config;

namespace Controll.IntegrationTests
{
    [DeploymentItem("Microsoft.Owin.Host.HttpListener.dll")]
    [TestClass]
    public class ServiceStressTest
    {
        private const string LocalHostUrl = "http://localhost:10244/"; // Change this to your hostname (or localhost but machine-name works with Fiddler)

        // Add user and zombie in datebase for mocked data is not exists
        private static bool _userAndZombieExists;
        private static readonly ISessionFactory Factory = NHibernateHelper.GetSessionFactoryForTesting();

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            if (!_userAndZombieExists)
            {
                using (var session = Factory.OpenSession())
                using (var transaction = session.BeginTransaction())
                {
                    var userRepo = new ControllUserRepository(session);
                    if (userRepo.GetByUserName("username") == null)
                    {
                        userRepo.Add(new ControllUser
                            {
                                Email = "email",
                                Password = "password",
                                UserName = "username"
                            });
                    }

                    var user = userRepo.GetByUserName("username");

                    if (user.GetZombieByName("zombieName") == null)
                    {
                        user.Zombies.Add(new Zombie
                            {
                                Name = "zombieName",
                                Owner = user
                            });
                    }

                    userRepo.Update(user);
                    transaction.Commit();
                }
                _userAndZombieExists = true;
            }
        }

        private void UseTestData()
        {
            Bootstrapper.Kernel.Rebind<ISession>()
                        .ToMethod(_ => Factory.OpenSession())
                        .InThreadScope();
        }

        [TestMethod]
        public async Task ShouldBeAbleToHandle100SimultaneousOfflinePingMessages()
        {
            var server = new ControllStandAloneServer("http://*:10244/");
            UseTestData();

            using (server.Start())
            {
                var client = new ControllClient(LocalHostUrl);
                client.Connect();
                client.LogOn("username", "password");
                // Now NHibernate is initialized and warm

                var tickets = new ConcurrentDictionary<Guid, bool>(); // bool is used to check if the zombie has recieved it

                const int range = 100;
                Enumerable.Range(0, range).AsParallel().ForAll(i =>
                    {
                        var ticket = client.Ping("zombieName");
                        Assert.AreNotEqual(ticket, Guid.Empty);

                        tickets.AddOrUpdate(ticket, false, (_, __) => false);
                    });

                Assert.AreEqual(range, tickets.Count);
                Assert.IsTrue(tickets.All(kp => kp.Key.Equals(Guid.Empty) == false));
                client.Disconnect();

                var zombie = new ControllZombieClient(LocalHostUrl);

                int reciveCount = 0;
                zombie.Pinged += (sender, args) =>
                    {
                        var updateResult = tickets.TryUpdate(args.Ticket, true, false);
                        Assert.IsTrue(updateResult);
                        reciveCount++;
                    };

                zombie.LogOn("username", "password", "zombieName");

                while (reciveCount < range)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                Console.Write("Asserting that all tickets have been recieved at zombie... ");
                Assert.IsTrue(tickets.All(kv => kv.Value));
                Console.WriteLine("OK!\n\n\n\n");
            }
        }

        [TestMethod]
        public async Task ShouldBeAbleToHandle100SimultaneousOnlinePingMessages()
        {
            var server = new ControllStandAloneServer("http://*:10244/");
            UseTestData();

            using (server.Start())
            {
                var tickets = new ConcurrentDictionary<Guid, bool>(); // bool is used to check if the zombie has recieved it

                var client = new ControllClient(LocalHostUrl);
                var zombie = new ControllZombieClient(LocalHostUrl);
                client.Connect();
                client.LogOn("username", "password");
                zombie.LogOn("username", "password", "zombieName");

                int reciveCount = 0;
                int reciveCount2 = 0;
                zombie.Pinged += (sender, args) =>
                    {
                        var updateResult = tickets.TryUpdate(args.Ticket, true, false);
                        Assert.IsTrue(updateResult);
                        reciveCount++;
                    };

                client.MessageDelivered += (sender, args) =>
                    {
                        Assert.IsTrue(tickets.ContainsKey(args.DeliveredTicket));

                        bool deliveredAtZombie;
                        Assert.IsTrue(tickets.TryGetValue(args.DeliveredTicket, out deliveredAtZombie));
                        Assert.IsTrue(deliveredAtZombie);

                        reciveCount2++;
                        Console.WriteLine("Client notified of delivery of ticket " + args.DeliveredTicket);
                    };

                const int range = 100;
                Enumerable.Range(0, range).AsParallel().ForAll(i =>
                    {
                        var ticket = client.Ping("zombieName");
                        Assert.AreNotEqual(ticket, Guid.Empty);

                        tickets.AddOrUpdate(ticket, false, (_, __) => false);
                    });

                Assert.AreEqual(range, tickets.Count);
                Assert.IsTrue(tickets.All(kp => kp.Key.Equals(Guid.Empty) == false));

                while (reciveCount < range  || reciveCount2 < range)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                Console.Write("Asserting that all tickets have been recieved at zombie... ");
                Assert.IsTrue(tickets.All(kv => kv.Value));
                Console.WriteLine("OK!\n\n\n\n");

                zombie.HubConnection.Stop();
                client.Disconnect();
            }
        }

        [TestMethod]
        public async Task ShouldBeAbleToHandle100SimultaneousOfflineInvocationMessages()
        {
            var server = new ControllStandAloneServer("http://*:10244/");
            UseTestData();

            using (server.Start())
            {
                var zombie = new ControllZombieClient(LocalHostUrl);
                var activityKey = Guid.NewGuid();
                zombie.LogOn("username", "password", "zombieName");
                zombie.Synchronize(new List<ActivityViewModel> { getActivity(activityKey) } ).Wait();
                zombie.SignOut().Wait();
                zombie.HubConnection.Stop(); // Make _sure_ it is not recieving anything

                var client = new ControllClient(LocalHostUrl);
                client.Connect();
                client.LogOn("username", "password");

                var tickets = new ConcurrentDictionary<Guid, bool>(); // bool is used to check if the zombie has recieved it

                const int range = 100;
                Enumerable.Range(0, range).AsParallel().ForAll(i =>
                {
                    var ticket = client.StartActivity("zombieName", activityKey, new Dictionary<string, string>(), "command");
                    Assert.AreNotEqual(ticket, Guid.Empty, "Returned invocation ticked was Guid.Empty. Run: " + i);

                    tickets.AddOrUpdate(ticket, false, (_, __) => false);
                });

                Assert.AreEqual(range, tickets.Count);
                Assert.IsTrue(tickets.All(kp => kp.Key.Equals(Guid.Empty) == false));
                client.Disconnect();
                
                int reciveCount = 0;
                zombie = new ControllZombieClient(LocalHostUrl);
                zombie.ActivateZombie += (sender, args) =>
                {
                    var updateResult = tickets.TryUpdate(args.ActivityTicket, true, false);
                    Assert.IsTrue(updateResult);
                    reciveCount++;
                };
                Assert.IsTrue(zombie.LogOn("username", "password", "zombieName"));

                TimeSpan totalWait = TimeSpan.FromSeconds(0);

                while (reciveCount < range)
                {
                    var wait = TimeSpan.FromSeconds(1);
                    await Task.Delay(wait);
                    totalWait += wait;

                    const double waitLimit = 15;
                    if (totalWait > TimeSpan.FromSeconds(waitLimit))
                        Assert.Fail("Did not recieve all invocations after " + waitLimit
                            + " seconds. Expected: " + range + ", gotten: " + reciveCount);
                }


                Console.Write("Asserting that all invocation tickets have been recieved at zombie... ");
                Assert.IsTrue(tickets.All(kv => kv.Value));
                Console.WriteLine("OK!\n\n\n\n");
            }
        }

        [TestMethod]
        public async Task ShouldBeAbleToHandleMassSimultaneousOnlineInvocationAndRecieveMessagesAndResults()
        {
            var server = new ControllStandAloneServer("http://*:10244/");
            UseTestData();

            using (server.Start())
            {
                var zombie = new ControllZombieClient(LocalHostUrl);
                var activityKey = Guid.NewGuid();
                var activity = getActivity(activityKey);
                zombie.LogOn("username", "password", "zombieName");
                zombie.Synchronize(new List<ActivityViewModel> { activity }).Wait();

                var client = new ControllClient(LocalHostUrl);
                client.Connect();
                client.LogOn("username", "password");

                var tickets = new ConcurrentDictionary<Guid, bool>(); // bool is used to check if the zombie has recieved it

                const int range = 50;

                int reciveCount = 0;
                int reciveCount2 = 0;

                zombie.ActivateZombie += (sender, args) =>
                {
                    Interlocked.Increment(ref reciveCount);
                    tickets.TryUpdate(args.ActivityTicket, true, false);
                };

                client.MessageDelivered += (sender, args) =>
                {
                    Interlocked.Increment(ref reciveCount2);
                };
                Enumerable.Range(0, range).AsParallel().ForAll(i =>
                {
                    var ticket = client.StartActivity("zombieName", activityKey, new Dictionary<string, string>(), "command");
                    Assert.AreNotEqual(ticket, Guid.Empty, "Returned invocation ticked was Guid.Empty. Run: " + i);

                    tickets.AddOrUpdate(ticket, false, (_, __) => false);
                });

                Assert.AreEqual(range, tickets.Count);
                Assert.IsTrue(tickets.All(kp => kp.Key.Equals(Guid.Empty) == false));
                
                TimeSpan totalWait = TimeSpan.FromSeconds(0);

                while (reciveCount < range || reciveCount2 < range)
                {
                    var wait = TimeSpan.FromSeconds(1);
                    await Task.Delay(wait);
                    totalWait += wait;

                    const double waitLimit = 5;
                    if (totalWait <= TimeSpan.FromSeconds(waitLimit)) continue;

                    if (reciveCount2 < range)
                    {
                        Assert.Fail("Did not recieve all message delivery acknowledgements after " + waitLimit + " seconds." +
                                    "\nExpected delivery acknowledgements: " + range + ", gotten: " + reciveCount2 +
                                    "\nExpected invocations: " + range + ", gotten: " + reciveCount);
                    }
                    else
                    {
                        Assert.Fail("Did not recieve all invocations after " + waitLimit + " seconds." +
                                    "\nExpected delivery acknowledgements: " + range + ", gotten: " + reciveCount2 +
                                    "\nExpected invocations: " + range + ", gotten: " + reciveCount);
                    }
                }

                Console.Write("Asserting that all invocation tickets have been recieved at zombie... ");
                Assert.IsTrue(tickets.All(kv => kv.Value));
                Console.WriteLine("OK!\n\n\n\n");

                Console.WriteLine("Now sending messages corresponding to all the invocations");

                int recieveCount3 = 0;

                client.ActivityMessageRecieved += (sender, args) => Interlocked.Increment(ref recieveCount3);
                tickets.AsParallel().ForAll(i =>
                {
                        zombie.ActivityNotify(i.Key, "notification");
                        zombie.ActivityCompleted(i.Key, "completed");
                    });

                totalWait = TimeSpan.FromSeconds(0);
                while (recieveCount3 < range * 2) // times 2 b/c we send both notification and completed
                {
                    var wait = TimeSpan.FromSeconds(1);
                    await Task.Delay(wait);
                    totalWait += wait;

                    const double waitLimit = 5;
                    if (totalWait <= TimeSpan.FromSeconds(waitLimit)) continue;

                    Assert.Fail("Did not recieve all activity messages after " + waitLimit + " seconds." +
                                "\nExpected activity messages: " + range * 2 + ", gotten: " + recieveCount3);
                }

                int recieveCount4 = 0;
                Console.WriteLine("All activity messages have been delivered to the caller successfully");
                Console.WriteLine("Now testing activity results");

                client.ActivityResultRecieved += (sender, args) => Interlocked.Increment(ref recieveCount4);
                tickets.AsParallel().ForAll(i =>
                    {
                        zombie.ActivityResult(i.Key, activity.Commands.First());
                    });

                totalWait = TimeSpan.FromSeconds(0);
                while (recieveCount4 < range)
                {
                    var wait = TimeSpan.FromSeconds(1);
                    await Task.Delay(wait);
                    totalWait += wait;

                    const double waitLimit = 5;
                    if (totalWait <= TimeSpan.FromSeconds(waitLimit)) continue;

                    Assert.Fail("Did not recieve all activity results after " + waitLimit + " seconds." +
                                "\nExpected activity results: " + range + ", gotten: " + recieveCount3);
                }
            }

        }

        [TestMethod]
        public async Task ShouldBeAbleToHandleNewConnectedClients()
        {
            var server = new ControllStandAloneServer("http://*:10244/");
            UseTestData();

            using (server.Start())
            {
                var zombie = new ControllZombieClient(LocalHostUrl);
                var activityKey = Guid.NewGuid();
                var activity = getActivity(activityKey);
                zombie.LogOn("username", "password", "zombieName");
                zombie.Synchronize(new List<ActivityViewModel> { activity }).Wait();
                
                const int range = 50;
                int reciveCount = 0;

                var resetEvent = new ManualResetEvent(false);
                zombie.ActivateZombie += (sender, args) => resetEvent.Set();

                var clients = new ControllClient[range];
                var timer = new Stopwatch();
                //¨å'XmlConfigurator.Configure();
                Enumerable.Range(0, range).ToList().ForEach(i =>
                    {
                        clients[i] = new ControllClient(LocalHostUrl);
                        clients[i].Connect();
                        if (!clients[i].LogOn("username", "password")) throw new AssertionFailure("Could not log on user " + i);
                        clients[i].MessageDelivered += (sender, args) =>
                        {
                            Interlocked.Increment(ref reciveCount);
                            Console.WriteLine("Got message at +" + timer.ElapsedMilliseconds + "ms");
                        };
                    });

                var session = (ISession)Bootstrapper.Kernel.GetService(typeof(ISession));
                var user = session.Get<ControllUser>(1);
                Assert.AreEqual(range, user.ConnectedClients.Count);

                var ticket = clients[0].StartActivity("zombieName", activity.Key, new Dictionary<string, string>(), "commandName");

                Assert.IsTrue(resetEvent.WaitOne(TimeSpan.FromSeconds(5)));

                timer.Start();
                zombie.ActivityNotify(ticket, "notify!");

                var maxWait = TimeSpan.FromSeconds(10);
                var wait = new TimeSpan(0);
                while (reciveCount < range)
                {
                    var delay = TimeSpan.FromSeconds(2);
                    await Task.Delay(delay);
                    wait += delay;

                    if (wait <= maxWait) continue;

                    timer.Stop();
                    Assert.Fail("Expected " + range + " messages but got " + reciveCount + " at time +" + timer.ElapsedMilliseconds + "ms");
                }
                timer.Stop();
            }
        }
        
        private ActivityViewModel getActivity(Guid key)
        {
            var mockedActivity = new ActivityViewModel
            {
                CreatorName = "name",
                Description = "mocked",
                Key = key,
                LastUpdated = DateTime.Now,
                Name = "Mocked Activity",
                Version = new Version(1, 2, 3, 4),
                Commands = new List<ActivityCommandViewModel>
                            {
                                new ActivityCommandViewModel
                                    {
                                        Label = "command-label",
                                        Name = "commandName",
                                        ParameterDescriptors = new List<ParameterDescriptorViewModel>
                                            {
                                                new ParameterDescriptorViewModel
                                                    {
                                                        Description = "pd-description",
                                                        IsBoolean = true,
                                                        Label = "pd-label",
                                                        Name = "pd-name",
                                                        PickerValues = new List<PickerValueViewModel>
                                                            {
                                                                new PickerValueViewModel
                                                                    {
                                                                        CommandName = "pv-commandname",
                                                                        Description = "pv-description",
                                                                        Identifier = "pv-id",
                                                                        IsCommand = true,
                                                                        Label = "pv-label",
                                                                        Parameters = new Dictionary<string, string>
                                                                            {
                                                                                {"param1", "value1"}
                                                                            }
                                                                    }
                                                            }

                                                        
                                                    }
                                            }
                                    }
                            }
            };

            return mockedActivity;
        }
    }
}

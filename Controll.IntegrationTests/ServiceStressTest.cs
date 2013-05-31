using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Controll.Client;
using Controll.Common;
using Controll.Common.Authentication;
using Controll.Common.ViewModels;
using Controll.Hosting.NHibernate;
using Controll.Zombie;
using NHibernate;
using NUnit.Framework;

namespace Controll.IntegrationTests
{
    public class ServiceStressTest : StandAloneFixtureBase
    {
        private const string LocalHostUrl = "http://erik-ws:10244/"; // Change this to your hostname (or localhost but machine-name works with Fiddler)
        

        [Test]
        public async Task ShouldBeAbleToHandle100SimultaneousOfflinePingMessages()
        {
            var auth = new DefaultAuthenticationProvider(LocalHostUrl);
            var client = new ControllClient(auth.Connect("username", "password").Result);
            client.SignIn().Wait();
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
            Assert.True(tickets.All(kp => kp.Key.Equals(Guid.Empty) == false));
            client.Disconnect();

            var zombie = new ZombieClient(LocalHostUrl);

            int reciveCount = 0;
            zombie.Pinged += (ticket) =>
                {
                    Interlocked.Increment(ref reciveCount);
                    zombie.ConfirmMessageDelivery(ticket);
                };

            zombie.Connect("username", "password", "zombieName").Wait();

            int waited = 0;
            while (reciveCount < range)
            {
                if (waited > 4)
                    break;
                await Task.Delay(TimeSpan.FromSeconds(1));
                waited++;
            }

            Console.Write("Asserting that all tickets have been recieved at zombie... ");
            Assert.AreEqual(range, reciveCount);
            Console.WriteLine("OK!\n\n\n\n");

            zombie.HubConnection.Stop();
        }

        [Test]
        public async Task ShouldBeAbleToHandle100SimultaneousOnlinePingMessages()
        {
            var auth = new DefaultAuthenticationProvider(LocalHostUrl);
            var client = new ControllClient(auth.Connect("username", "password").Result);
            var zombie = new ZombieClient(LocalHostUrl);

            client.SignIn().Wait();
            zombie.Connect("username", "password", "zombieName").Wait();

            int reciveCount = 0;
            int reciveCount2 = 0;

            zombie.Pinged += ticket =>
                {
                    Interlocked.Increment(ref reciveCount);
                    zombie.ConfirmMessageDelivery(ticket);
                };

            client.MessageDelivered += (sender, args) => Interlocked.Increment(ref reciveCount2);

            const int range = 100;
            Enumerable.Range(0, range).AsParallel().ForAll(i =>
                {
                    var ticket = client.Ping("zombieName");
                    Assert.AreNotEqual(ticket, Guid.Empty);
                });
            
            int wait = 0;
            while (reciveCount < range || reciveCount2 < range)
            {
                if (wait > 4)
                    break;
                await Task.Delay(TimeSpan.FromSeconds(1));
                wait++;
            }

            Console.Write("Asserting that all tickets have been recieved at zombie... ");
            Assert.True(reciveCount == range && reciveCount2 == range, "Expected {0} pings, got {1}. Expected {2} pongs, got {3}", range, reciveCount, range, reciveCount2);
            Console.WriteLine("OK!\n\n\n\n");

            zombie.HubConnection.Stop();
            client.Disconnect();
        }

        [Test]
        public async Task ShouldBeAbleToHandle100SimultaneousOfflineInvocationMessages()
        {
            var auth = new DefaultAuthenticationProvider(LocalHostUrl);

            var zombie = new ZombieClient(LocalHostUrl);
            zombie.Connect("username", "password", "zombieName").Wait();
            zombie.HubConnection.Stop(); // Make _sure_ it is not recieving anything

            var client = new ControllClient(auth.Connect("username", "password").Result);
            client.SignIn().Wait();

            var tickets = new ConcurrentDictionary<Guid, bool>(); // bool is used to check if the zombie has recieved it

            const int range = 100;
            Enumerable.Range(0, range).AsParallel().ForAll(i =>
                {
                    var ticket = client.StartActivity("zombieName", Activity.Id, new Dictionary<string, string>(), "command");
                    Assert.AreNotEqual(ticket, Guid.Empty);

                    tickets.AddOrUpdate(ticket, false, (_, __) => false);
                });

            Assert.AreEqual(range, tickets.Count);
            Assert.True(tickets.All(kp => kp.Key.Equals(Guid.Empty) == false));
            client.Disconnect();

            int reciveCount = 0;
            zombie = new ZombieClient(LocalHostUrl);
            zombie.InvocationRequest += (info) =>
                {
                    tickets.TryUpdate(info.Ticket, true, false);
                    zombie.ConfirmMessageDelivery(info.Ticket);
                    reciveCount++;
                };
            zombie.Connect("username", "password", "zombieName").Wait();

            TimeSpan totalWait = TimeSpan.FromSeconds(0);

            while (reciveCount < range)
            {
                var wait = TimeSpan.FromSeconds(1);
                await Task.Delay(wait);
                totalWait += wait;

                const double waitLimit = 15;
                if (totalWait > TimeSpan.FromSeconds(waitLimit))
                    throw new Exception(String.Format("Did not recieve all invocations after " + waitLimit
                                                      + " seconds. Expected: " + range + ", gotten: " + reciveCount));
            }


            Console.Write("Asserting that all invocation tickets have been recieved at zombie... ");
            Assert.True(tickets.All(kv => kv.Value));
            Console.WriteLine("OK!\n\n\n\n");
            zombie.HubConnection.Stop();
        }

        [Test]
        public async Task ShouldBeAbleToHandleMassSimultaneousOnlineInvocationAndRecieveMessagesAndResults()
        {
            var auth = new DefaultAuthenticationProvider(LocalHostUrl);
            var zombie = new ZombieClient(LocalHostUrl);

            zombie.Connect("username", "password", "zombieName").Wait();

            var client = new ControllClient(auth.Connect("username", "password").Result);
            client.SignIn().Wait();

            var tickets = new ConcurrentBag<Guid>(); // bool is used to check if the zombie has recieved it

            const int range = 50;

            int reciveCount = 0;
            int reciveCount2 = 0;

            zombie.InvocationRequest += (info) =>
                {
                    Interlocked.Increment(ref reciveCount);
                    zombie.ConfirmMessageDelivery(info.Ticket);
                };

            client.MessageDelivered += (sender, args) => { Interlocked.Increment(ref reciveCount2); };
            Enumerable.Range(0, range).AsParallel().ForAll(i =>
                {
                    var ticket = client.StartActivity("zombieName", Activity.Id, new Dictionary<string, string>(), "command");
                    tickets.Add(ticket);
                    Assert.AreNotEqual(ticket, Guid.Empty);
                });

            Assert.AreEqual(range, tickets.Count);
            Assert.True(tickets.All(kp => kp.Equals(Guid.Empty) == false));

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
                    throw new Exception("Did not recieve all message delivery acknowledgements after " + waitLimit + " seconds." +
                                        "\nExpected delivery acknowledgements: " + range + ", gotten: " + reciveCount2 +
                                        "\nExpected invocations: " + range + ", gotten: " + reciveCount);
                }
                else
                {
                    throw new Exception("Did not recieve all invocations after " + waitLimit + " seconds." +
                                        "\nExpected delivery acknowledgements: " + range + ", gotten: " + reciveCount2 +
                                        "\nExpected invocations: " + range + ", gotten: " + reciveCount);
                }
            }
            
            Console.WriteLine("Now sending messages corresponding to all the invocations");

            int recieveCount3 = 0;

            client.ActivityMessageRecieved += (sender, args) => Interlocked.Increment(ref recieveCount3);
            tickets.AsParallel().ForAll(i =>
                {
                    zombie.ActivityMessage(i, ActivityMessageType.Notification, "notification");
                    zombie.ActivityMessage(i, ActivityMessageType.Completed, "completed");
                });

            totalWait = TimeSpan.FromSeconds(0);
            while (recieveCount3 < range*2) // times 2 b/c we send both notification and completed
            {
                var wait = TimeSpan.FromSeconds(1);
                await Task.Delay(wait);
                totalWait += wait;

                const double waitLimit = 5;
                if (totalWait <= TimeSpan.FromSeconds(waitLimit)) continue;

                throw new Exception("Did not recieve all activity messages after " + waitLimit + " seconds." +
                                    "\nExpected activity messages: " + range*2 + ", gotten: " + recieveCount3);
            }

            int recieveCount4 = 0;
            Console.WriteLine("All activity messages have been delivered to the caller successfully");
            Console.WriteLine("Now testing activity results");

            client.ActivityResultRecieved += (sender, args) => Interlocked.Increment(ref recieveCount4);
            tickets.AsParallel().ForAll(i => zombie.ActivityResult(i, Activity.Commands.First()));

            totalWait = TimeSpan.FromSeconds(0);
            while (recieveCount4 < range)
            {
                var wait = TimeSpan.FromSeconds(1);
                await Task.Delay(wait);
                totalWait += wait;

                const double waitLimit = 5;
                if (totalWait <= TimeSpan.FromSeconds(waitLimit)) continue;

                throw new Exception("Did not recieve all activity results after " + waitLimit + " seconds." +
                                    "\nExpected activity results: " + range + ", gotten: " + recieveCount3);
            }
            zombie.HubConnection.Stop();
            client.Disconnect();
        }

        [Test]
        public void ShouldBeAbleToHandleNewConnectedClients()
        {
            // Use Nhibernate Profiler
            HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();

            var auth = new DefaultAuthenticationProvider(LocalHostUrl);

            var zombie = new ZombieClient(LocalHostUrl);
            zombie.Connect("username", "password", "zombieName").Wait();

            const int range = 50;
            int reciveCount = 0;

            var resetEvent = new ManualResetEvent(false);
            var resetEvent2 = new ManualResetEvent(false);
            var loginEvent = new ManualResetEvent(false);
            zombie.InvocationRequest += _ => resetEvent.Set();

            var connectionIdCollection = new ConcurrentDictionary<String, bool>();

            var clients = new ControllClient[range];
            var watch = new Stopwatch();
            watch.Start();
            Console.Write("Logging in all clients...");
            Enumerable.Range(0, range).AsParallel().ForAll(i =>
                {
                    clients[i] = new ControllClient(auth.Connect("username", "password").Result);
                    clients[i].ActivityMessageRecieved += (sender, args) =>
                        {
                            Interlocked.Increment(ref reciveCount);
                            if (!connectionIdCollection.TryUpdate(clients[i].HubConnection.ConnectionId, true, false))
                                throw new AssertionFailure("Could not update ConcurrentDictionary");

                            if (reciveCount == range)
                                resetEvent2.Set();
                        };
                    clients[i].SignIn().Wait();
                    
                    if (!connectionIdCollection.TryAdd(clients[i].HubConnection.ConnectionId, false))
                        throw new AssertionFailure("Could not insert into ConcurrentDictionary");

                    if (connectionIdCollection.Count() == range)
                    {
                        loginEvent.Set();
                        Console.WriteLine(" Every one is logged in at " + watch.ElapsedMilliseconds + " ms");
                    }
                });
            Assert.True(loginEvent.WaitOne(TimeSpan.FromSeconds(5)),
                        "Did not login all clients. Expected " + range + " but got " + connectionIdCollection.Count()); // log in all

            var ticket = clients[0].StartActivity("zombieName", Activity.Id, new Dictionary<string, string>(), "commandName");

            Assert.True(resetEvent.WaitOne(TimeSpan.FromSeconds(7)));

            zombie.ActivityMessage(ticket, ActivityMessageType.Notification, "notify!");

            var hasRecievedCorrectNumberOfMessages = resetEvent2.WaitOne(TimeSpan.FromSeconds(2));

            if (!hasRecievedCorrectNumberOfMessages)
            {
                Console.WriteLine("Expected " + range + " messages but got " + reciveCount);
                throw new Exception("Expected " + range + " messages but got " + reciveCount);
            }

            foreach(var client in clients)
                client.Disconnect();
            zombie.HubConnection.Stop();
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class QueueItemRepositoryTests : TestBase
    {
        [TestMethod]
        public void ShouldBeAbleToGetAllUndeliveredQueueItems()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var zombieRepo = new GenericRepository<Zombie>(session);
                var zombie = new Zombie
                    {
                        Name = "zombieName"
                    };
                zombieRepo.Add(zombie);

                var repo = new QueueItemRepostiory(session);

                var queueItem1 = new PingQueueItem
                    {
                        Reciever = zombie,
                        RecievedAtCloud = DateTime.Now
                    };

                var queueItem2 = new PingQueueItem
                    {
                        Reciever = zombie,
                        RecievedAtCloud = DateTime.Now,
                        Delivered = DateTime.Now
                    };

                repo.Add(queueItem1);
                repo.Add(queueItem2);

                var undelivered = repo.GetUndeliveredQueueItemsForZombie(zombie.Id);

                Assert.AreEqual(1, undelivered.Count);
                Assert.AreEqual(queueItem1, undelivered.ElementAt(0));
            }
        }
    }
}

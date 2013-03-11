using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class InMemoryRepositoryTests
    {
        [TestMethod]
        public void ShouldBeAbleToAddAndGet()
        {
            var repo = new InMemoryRepository<Activity>();
            var id = Guid.NewGuid();
            repo.Add(new Activity { Name = "name", Id = id });

            var activity = repo.Get(id);

            Assert.IsNotNull(activity);
            Assert.AreEqual(activity.Name, "name");
        }

        [TestMethod]
        public void ShouldBeAbleToAddAndRemove()
        {
            var repo = new InMemoryRepository<Activity>();
            var id = Guid.NewGuid();
            var activity = new Activity {Name = "name", Id = id};
            repo.Add(activity);

            repo.Remove(activity);

            var fetched = repo.Get(id);

            Assert.IsNull(fetched);
        }

        [TestMethod]
        public void ShouldBeAbleToAddGetUserFromUserName()
        {
            var repo = new InMemoryControllUserRepository();

            var user = new ControllUser() { UserName = "name", EMail = "mail", Id = 222 };
            repo.Add(user);

            var fetched = repo.GetByUserName("name");

            Assert.IsNotNull(fetched);
            Assert.AreEqual(user.UserName, fetched.UserName);
            Assert.AreEqual(user.Id, fetched.Id);
            Assert.AreEqual(user.EMail, fetched.EMail);
        }

        [TestMethod]
        public void ShouldBeAbleToAddGetUserFromEMail()
        {
            var repo = new InMemoryControllUserRepository();

            var user = new ControllUser() { UserName = "name", EMail = "mail", Id = 222 };
            repo.Add(user);

            var fetched = repo.GetByEMail("mail");

            Assert.IsNotNull(fetched);
            Assert.AreEqual(user.UserName, fetched.UserName);
            Assert.AreEqual(user.Id, fetched.Id);
            Assert.AreEqual(user.EMail, fetched.EMail);
        }

        [TestMethod]
        public void ShouldBeAbleToAddGetUserFromZombieConnectionId()
        {
            var repo = new InMemoryControllUserRepository();

            var user = new ControllUser
                {
                    UserName = "name",
                    EMail = "mail",
                    Id = 222,
                    ConnectedClients = new List<ControllClient> { new ControllClient { ConnectionId = "conn", DeviceType = DeviceType.PC} }
                };
            repo.Add(user);

            var fetched = repo.GetByConnectionId("conn");

            Assert.IsNotNull(fetched);
            Assert.AreEqual(user.UserName, fetched.UserName);
            Assert.AreEqual(user.Id, fetched.Id);
            Assert.AreEqual(user.EMail, fetched.EMail);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class ControllUserRepositoryTests : TestBase
    {
        [TestMethod]
        public void ShouldBeAbleToAddAndGetFromUserName()
        {
            using(var session = SessionFactory.OpenSession())
            using(session.BeginTransaction())
            {
                var repo = new ControllUserRepository(session);

                var user = new ControllUser
                    {
                        UserName = "name",
                        EMail = "mail",
                        Id = 222,
                        ConnectedClients = new List<ControllClient> { new ControllClient { ConnectionId = "conn", DeviceType = DeviceType.PC } }
                    };
                repo.Add(user);

                var fetched = repo.GetByConnectionId("conn");

                Assert.IsNotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.EMail, fetched.EMail);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddGetUserFromUserName()
        {
            using(var session = SessionFactory.OpenSession())
            using(session.BeginTransaction())
            {
                var repo = new ControllUserRepository(session);

                var user = new ControllUser() { UserName = "name", EMail = "mail", Id = 222 };
                repo.Add(user);

                var fetched = repo.GetByUserName("name");

                Assert.IsNotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.EMail, fetched.EMail);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddGetUserFromEMail()
        {
            using(var session = SessionFactory.OpenSession())
            using(session.BeginTransaction())
            {
                var repo = new ControllUserRepository(session);

                var user = new ControllUser() { UserName = "name", EMail = "mail", Id = 222 };
                repo.Add(user);

                var fetched = repo.GetByEMail("mail");

                Assert.IsNotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.EMail, fetched.EMail);
            }
        }
    }
}

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
        public void ShouldBeAbleToAddAndGetFromConnectionId()
        {
            using(var session = SessionFactory.OpenSession())
            using(session.BeginTransaction())
            {
                var repo = new ControllUserRepository(session);

                var user = new ControllUser
                    {
                        UserName = "name",
                        Email = "mail",
                        Password = "password"
                    };
                repo.Add(user);

                user.ConnectedClients.Add(new ControllClient { ConnectionId = "conn", ClientCommunicator = user});

                repo.Update(user);
                
                var fetched = repo.GetByConnectionId("conn");

                Assert.IsNotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.Email, fetched.Email);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddGetUserFromUserName()
        {
            using(var session = SessionFactory.OpenSession())
            using(session.BeginTransaction())
            {
                var repo = new ControllUserRepository(session);

                var user = new ControllUser()
                {
                    UserName = "name",
                    Email = "mail",
                    Id = 222,
                    Password = "password"
                };
                repo.Add(user);

                var fetched = repo.GetByUserName("name");

                Assert.IsNotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.Email, fetched.Email);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddGetUserFromEMail()
        {
            using(var session = SessionFactory.OpenSession())
            using(session.BeginTransaction())
            {
                var repo = new ControllUserRepository(session);

                var user = new ControllUser()
                {
                    UserName = "name",
                    Email = "mail",
                    Id = 222,
                    Password = "password"
                };
                repo.Add(user);

                var fetched = repo.GetByEMail("mail");

                Assert.IsNotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.Email, fetched.Email);
            }
        }
    }
}

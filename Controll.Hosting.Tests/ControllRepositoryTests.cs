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
    public class ControllRepositoryTests : TestBase
    {
        [TestMethod]
        public void ShouldBeAbleToAddAndGetFromConnectionId()
        {
            using(var session = SessionFactory.OpenSession())
            using(session.BeginTransaction())
            {
                var repo = new ControllRepository(session);

                var user = new ControllUser
                    {
                        UserName = "name",
                        Email = "mail",
                        Password = "password"
                    };
                session.Save(user);

                user.ConnectedClients.Add(new ControllClient { ConnectionId = "conn", ClientCommunicator = user});

                session.Update(user);
                
                var fetched = repo.GetClientByConnectionId<ControllUser>("conn");

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
                var repo = new ControllRepository(session);

                var user = new ControllUser()
                {
                    UserName = "name",
                    Email = "mail",
                    Id = 222,
                    Password = "password"
                };
                session.Save(user);

                var fetched = repo.GetUserFromUserName("name");

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
                var repo = new ControllRepository(session);

                var user = new ControllUser()
                {
                    UserName = "name",
                    Email = "mail",
                    Id = 222,
                    Password = "password"
                };
                session.Save(user);

                var fetched = repo.GetUserFromEmail("mail");

                Assert.IsNotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.Email, fetched.Email);
            }
        }
    }
}

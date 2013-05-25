using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    
    public class ControllRepositoryTests : TestBase
    {
        [Test]
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

                Assert.NotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.Email, fetched.Email);
            }
        }

        [Test]
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

                Assert.NotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.Email, fetched.Email);
            }
        }

        [Test]
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

                Assert.NotNull(fetched);
                Assert.AreEqual(user.UserName, fetched.UserName);
                Assert.AreEqual(user.Id, fetched.Id);
                Assert.AreEqual(user.Email, fetched.Email);
            }
        }
    }
}

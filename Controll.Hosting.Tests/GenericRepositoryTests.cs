﻿using System;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class GenericRepositoryTests : TestBase
    {
        [TestMethod]
        public void ShouldBeAbleToAddAndPersistEntity()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var repo = new GenericRepository<ControllUser>(session);

                var user = new ControllUser()
                    {
                        EMail = "email",
                        Password = "password",
                        UserName = "username"
                    };

                repo.Add(user);

                var user2 = repo.Get(user.Id);

                Assert.AreEqual(user2.EMail, "email");
                Assert.AreEqual(user2.Password, "password");
                Assert.AreEqual(user2.UserName, "username");
            }
        }

        [TestMethod]
        public void ShouldBeAbleToRemoveEntity()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var repo = new GenericRepository<ControllUser>(session);

                var user = new ControllUser()
                    {
                        EMail = "emailToRemove",
                        Password = "password",
                        UserName = "userToRemove"
                    };

                repo.Add(user);

                repo.Remove(user);

                var user2 = repo.Get(user.Id);

                Assert.IsNull(user2);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToUpdateEntity()
        {
            using(var session = SessionFactory.OpenSession())
            using(session.BeginTransaction())
            {
                var repo = new GenericRepository<ControllUser>(session);
                var user = new ControllUser()
                    {
                        EMail = "email",
                        Password = "password",
                        UserName = "username"
                    };

                repo.Add(user);

                user.EMail = "hehe";
                repo.Update(user);

                var user2 = repo.Get(user.Id);

                Assert.AreEqual(user2.EMail, "hehe");
            }
        }
    }
}

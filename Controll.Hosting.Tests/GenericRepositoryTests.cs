using System;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class GenericRepositoryTests : TestBase
    {
<<<<<<< HEAD
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
=======
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            Console.WriteLine("Initialize");
            NHibernateHelper.IsInTesting = true;
            NHibernateHelper.OpenSession().Dispose();
            NHibernateHelper.ClearDb();
        }

        [TestMethod]
        public void ShouldBeAbleToAddAndPersistEntity()
        {
            var repo = new GenericRepository<ControllUser>();

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
            NHibernateHelper.ClearDb();
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }

        [TestMethod]
        public void ShouldBeAbleToRemoveEntity()
        {
<<<<<<< HEAD
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
=======
            var repo = new GenericRepository<ControllUser>();

            var user = new ControllUser()
            {
                EMail = "email",
                Password = "password",
                UserName = "username"
            };

            repo.Add(user);

            repo.Remove(user);

            var user2 = repo.Get(user.Id);

            Assert.IsNull(user2);
            NHibernateHelper.ClearDb();
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }

        [TestMethod]
        public void ShouldBeAbleToUpdateEntity()
        {
<<<<<<< HEAD
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
=======
            var repo = new GenericRepository<ControllUser>();

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
            NHibernateHelper.ClearDb();
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }
    }
}

using System;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentNHibernate.Testing;
using NHibernate;


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
                new PersistenceSpecification<ControllUser>(session)
                    .CheckProperty(x => x.EMail, "email")
                    .CheckProperty(x => x.Password, "password")
                    .CheckProperty(x => x.UserName, "username")
                    .VerifyTheMappings();
        }

        [TestMethod]
        public void ShouldBeAbleToAddGetAll()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var objects = Builder<Activity>
                    .CreateListOfSize(50)
                    .All()
                    .With(x => x.Id = Guid.NewGuid())
                    .Build();

                var repo = new GenericRepository<Activity>(session);

                foreach(var obj in objects)
                    repo.Add(obj);

                var fetched = repo.GetAll(33);

                Assert.AreEqual(33, fetched.Count);
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

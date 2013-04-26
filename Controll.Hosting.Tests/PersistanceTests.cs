﻿using System;
using System.Collections.Generic;
using System.Linq;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentNHibernate.Testing;
using NHibernate;


namespace Controll.Hosting.Tests
{
    [TestClass]
    public class PersistanceTests : TestBase
    {
        [TestMethod]
        public void ShouldBeAbleToAddAndPersistControllUser()
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
        public void ShouldBeAbleToAddAndPersistZombie()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var zombie = new Zombie {ConnectionId = "conn", Id = 123, Name = "name"};
                var repo = new GenericRepository<Zombie>(session);
                repo.Add(zombie);

                session.Evict(zombie);

                var gotten = repo.Get(zombie.Id);
                
                Assert.AreEqual(zombie.Name, gotten.Name);
                Assert.AreEqual(zombie.Id, gotten.Id);
                Assert.AreEqual(zombie.ConnectionId, gotten.ConnectionId);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddAndPersistZombieToControllUser()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var zombie = new Zombie { ConnectionId = "conn", Id = 123, Name = "name" };
                var user = new ControllUser {UserName = "user", EMail = "mail", Password = "password", Zombies = new List<Zombie>()};

                var userRepo = new GenericRepository<ControllUser>(session);
                userRepo.Add(user);
                var id = user.Id;

                user.Zombies.Add(zombie);

                userRepo.Update(user);

                user = null;

                var gotten = userRepo.Get(id);

                Assert.AreEqual(zombie.Name, gotten.Zombies[0].Name);
                Assert.AreEqual(zombie.Id, gotten.Zombies[0].Id);
                Assert.AreEqual(zombie.ConnectionId, gotten.Zombies[0].ConnectionId);
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddAndPersistActivityDeep()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var activity = TestingHelper.GetListOfZombies()[0].Activities[0]; // Get generated activity with commands and parameters
                var activityCopy = TestingHelper.GetListOfZombies()[0].Activities[0]; // This is used for comparing since the one above will be the same reference when getting from repo
                var repo = new GenericRepository<Activity>(session);
                repo.Add(activity);
                
                var activityGotten = repo.Get(activity.Id);

                Assert.AreNotSame(activityGotten, activityCopy); // Check not same reference

                Assert.AreEqual(activityCopy.CreatorName, activityGotten.CreatorName);
                Assert.AreEqual(activityCopy.Description, activityGotten.Description);
                Assert.AreEqual(activityCopy.FilePath, activityGotten.FilePath);
                Assert.AreEqual(activityCopy.LastUpdated, activityGotten.LastUpdated);
                Assert.AreEqual(activityCopy.Name, activityGotten.Name);
                Assert.AreEqual(activityCopy.Version, activityGotten.Version);

                Assert.AreEqual(activityCopy.Commands.Count, activityGotten.Commands.Count);
                for (int c = 0; c < activityCopy.Commands.Count; c++)
                {
                    var command = activityCopy.Commands[c];
                    var commandGotten = activityGotten.Commands[c];

                    Assert.AreEqual(command.Id, commandGotten.Id);
                    Assert.AreEqual(command.Label, commandGotten.Label);
                    Assert.AreEqual(command.Name, commandGotten.Name);

                    Assert.AreEqual(command.ParameterDescriptors.Count, commandGotten.ParameterDescriptors.Count);
                    for (int pd = 0; pd < command.ParameterDescriptors.Count; pd++)
                    {
                        var param = command.ParameterDescriptors[pd];
                        var paramGotten = commandGotten.ParameterDescriptors[pd];

                        Assert.AreEqual(param.Description, paramGotten.Description);
                        Assert.AreEqual(param.Id, paramGotten.Id);
                        Assert.AreEqual(param.Label, paramGotten.Label);
                        Assert.AreEqual(param.Name, paramGotten.Name);

                        CollectionAssert.AreEquivalent(param.PickerValues.ToList(), paramGotten.PickerValues.ToList());
                    }
                }
            }
        }

        [TestMethod]
        public void ShouldBeAbleToUpdateAndPersistZombe()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var activities = TestingHelper.GetListOfZombies()[0].Activities;
                var activitiesCopy = TestingHelper.GetListOfZombies()[0].Activities;
                var zombie = new Zombie
                    {
                        ConnectionId = "conn",
                        Id = 123,
                        Name = "name"
                    };
                var zombieCopy = new Zombie
                {
                    ConnectionId = "conn",
                    Id = 123,
                    Name = "name",
                    Activities = activitiesCopy
                };

                var zombieRepo = new GenericRepository<Zombie>(session);
                var activityRepo = new GenericRepository<Activity>(session);
                
                // Add everything to repo
                zombieRepo.Add(zombie);
                foreach(var activity in activities)
                    activityRepo.Add(activity);

                // Set zombies activities and update DB
                zombie.Activities = activities;
                zombieRepo.Update(zombie);
                
                var gotten = zombieRepo.Get(zombie.Id);

                Assert.AreNotSame(zombieCopy, gotten);
                Assert.AreEqual(zombieCopy.Activities.Count, gotten.Activities.Count);
                for (int i = 0; i < zombieCopy.Activities.Count; i++)
                {
                    var activity = zombieCopy.Activities[i];
                    var activityGotten = gotten.Activities[i];

                    Assert.AreEqual(activity.CreatorName, activityGotten.CreatorName);
                    Assert.AreEqual(activity.Description, activityGotten.Description);
                    Assert.AreEqual(activity.FilePath, activityGotten.FilePath);
                    Assert.AreEqual(activity.LastUpdated, activityGotten.LastUpdated);
                    Assert.AreEqual(activity.Name, activityGotten.Name);
                    Assert.AreEqual(activity.Version, activityGotten.Version);

                    Assert.AreEqual(activity.Commands.Count, activityGotten.Commands.Count);
                    for (int c = 0; c < activity.Commands.Count; c++)
                    {
                        var command = activity.Commands[c];
                        var commandGotten = activityGotten.Commands[c];

                        Assert.AreEqual(command.Id, commandGotten.Id);
                        Assert.AreEqual(command.Label, commandGotten.Label);
                        Assert.AreEqual(command.Name, commandGotten.Name);

                        Assert.AreEqual(command.ParameterDescriptors.Count, commandGotten.ParameterDescriptors.Count);
                        for (int pd = 0; pd < command.ParameterDescriptors.Count; pd++)
                        {
                            var param = command.ParameterDescriptors[pd];
                            var paramGotten = commandGotten.ParameterDescriptors[pd];

                            Assert.AreEqual(param.Description, paramGotten.Description);
                            Assert.AreEqual(param.Id, paramGotten.Id);
                            Assert.AreEqual(param.Label, paramGotten.Label);
                            Assert.AreEqual(param.Name, paramGotten.Name);

                            CollectionAssert.AreEquivalent(param.PickerValues.ToList(), paramGotten.PickerValues.ToList());
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddAndPersistActivity()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                new PersistenceSpecification<Activity>(session)
                    .CheckProperty(x => x.CreatorName, "name")
                    .CheckProperty(x => x.Description, "desc")
                    .CheckProperty(x => x.FilePath, "C:\\")
                    .CheckProperty(x => x.LastUpdated, DateTime.Parse("2012-12-12 12:12:12"))
                    .CheckProperty(x => x.Name, "activity name")
                    .VerifyTheMappings();
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddAndPersistPingQueueItem()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                new PersistenceSpecification<PingQueueItem>(session)
                    .CheckProperty(x => x.Delivered, DateTime.Parse("2004-03-11 13:22:11"))
                    .CheckProperty(x => x.RecievedAtCloud, DateTime.Parse("2004-03-11 13:22:11"))
                    .CheckProperty(x => x.TimeOut, 20)
                    .CheckProperty(x => x.SenderConnectionId, "connection-id")
                    .VerifyTheMappings();
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddAndPersistCommand()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                new PersistenceSpecification<ActivityCommand>(session)
                    .CheckProperty(x => x.Name, "name")
                    .CheckProperty(x => x.Label, "Label")
                    .VerifyTheMappings();
            }
        }

        [TestMethod]
        public void ShouldBeAbleToAddAndPersistParameterDescriptor()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                new PersistenceSpecification<ParameterDescriptor>(session)
                    .CheckProperty(x => x.Description, "description")
                    .CheckProperty(x => x.Label, "label")
                    .CheckProperty(x => x.Name, "name")
                    .CheckComponentList(x => x.PickerValues, new[] { "picker 1", "picker 2" })
                    .VerifyTheMappings();
            }
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

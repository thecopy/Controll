using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using FizzWare.NBuilder;
using NUnit.Framework;
using FluentNHibernate.Testing;
using NHibernate;
using NHibernate.Proxy;


namespace Controll.Hosting.Tests
{
    
    public class PersistanceTests : TestBase
    {
        public class PersistenceSpecificationEqualityComparer : IEqualityComparer
        {
            private readonly ISession _session;

            public PersistenceSpecificationEqualityComparer(ISession session)
            {
                _session = session;
            }

            public PersistenceSpecificationEqualityComparer( )
            {
            }

            private readonly Dictionary<Type, Delegate> _comparers = new Dictionary<Type, Delegate>();

            public void RegisterComparer<T>(Func<T, object> comparer)
            {
                _comparers.Add(typeof(T), comparer);
            }

            public new bool Equals(object x, object y)
            {
                if (x == null || y == null)
                {
                    return false;
                }

                if (_session != null)
                {
                    if (x as INHibernateProxy != null)
                    {
                        if (!NHibernateUtil.IsInitialized(x))
                            NHibernateUtil.Initialize(x);
                        x = _session.GetSessionImplementation().PersistenceContext.Unproxy(x);
                    }

                    if (y as INHibernateProxy != null)
                    {
                        if (!NHibernateUtil.IsInitialized(y))
                            NHibernateUtil.Initialize(y);
                        y = _session.GetSessionImplementation().PersistenceContext.Unproxy(y);
                    }
                }

                var xType = x.GetType();
                var yType = y.GetType();

                // check subclass to handle proxies
                if (_comparers.ContainsKey(xType) && (xType == yType || (yType.IsSubclassOf(xType) || xType.IsSubclassOf(yType))))
                {
                    var comparer = _comparers[xType];
                    var xValue = comparer.DynamicInvoke(new[] { x });
                    var yValue = comparer.DynamicInvoke(new[] { y });
                    return xValue.Equals(yValue);
                }
                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistControllUser()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
                new PersistenceSpecification<ControllUser>(session)
                    .CheckProperty(x => x.Email, "email")
                    .CheckProperty(x => x.Password, "password")
                    .CheckProperty(x => x.UserName, "username")
                    .VerifyTheMappings();
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistControllUserLogs()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var date = DateTime.Now;
                var ticket = Guid.NewGuid();
                var activity = new Activity()
                    {
                        Name = "activity",
                        LastUpdated = date,
                        Version = new Version(1, 0, 0, 0)
                    };
                var user = new ControllUser
                    {
                        UserName = "username",
                        Email = "email",
                        Password = "password"
                    };

                session.Save(user);
                session.Save(activity);

                user.LogBooks = new List<LogBook>
                    {
                        new LogBook
                            {
                                InvocationTicket = ticket,
                                Activity = activity,
                                LogMessages = new List<LogMessage>
                                    {
                                        new LogMessage
                                            {
                                                Date = date,
                                                Message = "message",
                                                Type = ActivityMessageType.Notification
                                            }
                                    }
                            }
                    };

                session.Update(user);

                int id = user.Id;
                user = null;
                user = session.Get<ControllUser>(id);

                Assert.AreEqual(1, user.LogBooks.Count);
                var book = user.LogBooks[0];
                Assert.AreEqual(ticket, book.InvocationTicket);
                Assert.AreEqual(activity.Id, book.Activity.Id);

                Assert.AreEqual(1, book.LogMessages.Count);
                var message = book.LogMessages[0];

                Assert.AreEqual("message", message.Message);
                Assert.AreEqual(ActivityMessageType.Notification, message.Type);
                Assert.AreEqual(date, message.Date);
            }
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistZombie()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var user = new ControllUser
                    {
                        UserName = "username",
                        Password = "password",
                        Email = "email"
                    };
                var zombie = new Zombie
                    {
                        Id = 123, 
                        Name = "name",
                        Owner = user
                    };
                zombie.ConnectedClients.Add(new ControllClient { ConnectionId = "conn" });

                session.Save(user);
                session.Save(zombie);

                session.Evict(zombie);

                var gotten = session.Get<Zombie>(zombie.Id);
                Assert.AreNotSame(zombie,gotten);
                Assert.AreEqual(zombie.Name, gotten.Name);
                Assert.AreEqual(zombie.Id, gotten.Id);
                Assert.True(zombie.ConnectedClients.Any(c => c.ConnectionId == "conn"));
            }
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistZombieToControllUser()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var zombie = new Zombie
                {
                    Id = 123,
                    Name = "name"
                };
                zombie.ConnectedClients.Add(new ControllClient { ConnectionId = "conn" });
                var user = new ControllUser {UserName = "user", Email = "mail", Password = "password", Zombies = new List<Zombie>()};

                session.Save(user);
                var id = user.Id;

                user.Zombies.Add(zombie);

                session.Update(user);

                var gotten = session.Get<ControllUser>(id);

                Assert.AreEqual(zombie.Name, gotten.Zombies[0].Name);
                Assert.AreEqual(zombie.Id, gotten.Zombies[0].Id);
                Assert.True(zombie.ConnectedClients.Any(c => c.ConnectionId == "conn"));
            }
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistActivityDeep()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var activity = TestingHelper.GetListOfZombies(1)[0].Activities[0]; // Get generated activity with commands and parameters
                var activityCopy = TestingHelper.GetListOfZombies(1)[0].Activities[0]; // Get a new copy of the activity so nhibernate wont track it 

                session.Save(activity);
                
                var activityGotten = session.Get<Activity>(activity.Id);

                Assert.AreNotSame(activityGotten, activityCopy); // Check not same reference

                Assert.AreEqual(activityCopy.CreatorName, activityGotten.CreatorName);
                Assert.AreEqual(activityCopy.Description, activityGotten.Description);
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
                        Assert.AreEqual(param.IsBoolean, paramGotten.IsBoolean);
                        Assert.AreEqual(param.Name, paramGotten.Name);

                        Assert.AreEqual(param.PickerValues.Count, paramGotten.PickerValues.Count);
                        for (int pv = 0; pv < param.PickerValues.Count; pv++)
                        {
                            var pickerValue = param.PickerValues[pv];
                            var pickerValueGotten = paramGotten.PickerValues[pv];

                            Assert.AreEqual(pickerValue.Description, pickerValueGotten.Description);
                            Assert.AreEqual(pickerValue.Id, pickerValueGotten.Id);
                            Assert.AreEqual(pickerValue.Label, pickerValueGotten.Label);
                            Assert.AreEqual(pickerValue.Identifier, pickerValueGotten.Identifier);
                            Assert.AreEqual(pickerValue.IsCommand, pickerValueGotten.IsCommand);


                            AssertionHelper.AssertDictionariesAreEqual(pickerValue.Parameters, pickerValueGotten.Parameters);
                        }
                    }
                }
            }
        }

        [Test]
        public void ShouldBeAbleToUpdateAndPersistZombe()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var activities = TestingHelper.GetListOfZombies(1)[0].Activities;
                var activitiesCopy = TestingHelper.GetListOfZombies(1)[0].Activities;

                var user = new ControllUser
                {
                    UserName = "username",
                    Password = "password",
                    Email = "email"
                };
                var zombie = new Zombie
                {
                    Id = 123,
                    Name = "name",
                    Owner = user
                };

                var zombieCopy = new Zombie
                {
                    Id = 123,
                    Name = "name",
                    Owner = user,
                    Activities = activitiesCopy
                };
                zombie.ConnectedClients.Add(new ControllClient { ConnectionId = "conn" });
                zombieCopy.ConnectedClients.Add(new ControllClient { ConnectionId = "conn" });
                
                session.Save(user);
                
                // Add everything to repo
                session.Save(zombie);
                foreach(var activity in activities)
                    session.Save(activity);

                // Set zombies activities and update DB
                zombie.Activities = activities;
                session.Update(zombie);
                
                var gotten = session.Get<Zombie>(zombie.Id);

                Assert.AreNotSame(zombieCopy, gotten);
                Assert.AreEqual(zombieCopy.Activities.Count, gotten.Activities.Count);
                for (int i = 0; i < zombieCopy.Activities.Count; i++)
                {
                    var activity = zombieCopy.Activities[i];
                    var activityGotten = gotten.Activities[i];

                    Assert.AreEqual(activity.CreatorName, activityGotten.CreatorName);
                    Assert.AreEqual(activity.Description, activityGotten.Description);
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
                            Assert.AreEqual(param.IsBoolean, paramGotten.IsBoolean);
                            Assert.AreEqual(param.Name, paramGotten.Name);

                            Assert.AreEqual(param.PickerValues.Count, paramGotten.PickerValues.Count);
                            for (int pv = 0; pv < param.PickerValues.Count; pv++)
                            {
                                var pickerValue = param.PickerValues[pv];
                                var pickerValueGotten = paramGotten.PickerValues[pv];

                                Assert.AreEqual(pickerValue.Description, pickerValueGotten.Description);
                                Assert.AreEqual(pickerValue.Id, pickerValueGotten.Id);
                                Assert.AreEqual(pickerValue.Label, pickerValueGotten.Label);
                                Assert.AreEqual(pickerValue.Identifier, pickerValueGotten.Identifier);
                                Assert.AreEqual(pickerValue.IsCommand, pickerValueGotten.IsCommand);


                                AssertionHelper.AssertDictionariesAreEqual(pickerValue.Parameters, pickerValueGotten.Parameters);
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistActivity()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                new PersistenceSpecification<Activity>(session)
                    .CheckProperty(x => x.CreatorName, "name")
                    .CheckProperty(x => x.Description, "desc")
                    .CheckProperty(x => x.LastUpdated, DateTime.Parse("2012-12-12 12:12:12"))
                    .CheckProperty(x => x.Name, "activity name")
                    .VerifyTheMappings();
            }
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistPingQueueItem()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var user = new ControllUser {UserName = "username", Email = "email", Password = "password"};

                session.Save(user);

                var comparer = new PersistenceSpecificationEqualityComparer(session);
                comparer.RegisterComparer((ControllUser x) => x.Id);
                comparer.RegisterComparer((ClientCommunicator x) => x.Id);
                
                new PersistenceSpecification<PingQueueItem>(session, comparer)
                    .CheckProperty(x => x.Delivered, DateTime.Parse("2004-03-11 13:22:11"))
                    .CheckProperty(x => x.RecievedAtCloud, DateTime.Parse("2004-03-11 13:22:11"))
                    .CheckReference(x => x.Sender, user)
                    .CheckReference(x => x.Reciever, user)
                    .VerifyTheMappings();
            }
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistActivityResultQueueItem()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var user = new ControllUser { UserName = "username", Email = "email", Password = "password" };
                var activity = new ActivityCommand
                    {
                        Name = "some-intermidiate-command"
                    };
                session.Save(user);

                var comparer = new PersistenceSpecificationEqualityComparer(session);
                comparer.RegisterComparer((ControllUser x) => x.Id);
                comparer.RegisterComparer((ClientCommunicator x) => x.Id);
                comparer.RegisterComparer((ActivityCommand x) => x.Name);

                var ticket = Guid.NewGuid();
                new PersistenceSpecification<ActivityResultQueueItem>(session, comparer)
                    .CheckProperty(x => x.Delivered, DateTime.Parse("2004-03-11 13:22:11"))
                    .CheckProperty(x => x.RecievedAtCloud, DateTime.Parse("2004-03-11 13:22:11"))
                    .CheckReference(x => x.Sender, user)
                    .CheckReference(x => x.Reciever, user)
                    .CheckReference(x => x.ActivityCommand, activity)
                    .CheckProperty(x => x.InvocationTicket, ticket)
                    .VerifyTheMappings();
            }
        }

        [Test]
        public void ShouldBeAbleToAddAndPersistActivityInvocationQueueItem()
        {
            using (var session = SessionFactory.OpenSession())
            using (session.BeginTransaction())
            {
                var user = new ControllUser { UserName = "username", Email = "email", Password = "password" };
                var activity = new Activity { Name = "mocked", Description = "desc", LastUpdated = DateTime.Parse("2004-03-11 13:22:11")};
                
                session.Save(user);
                session.Save(activity);
                
                var comparer = new PersistenceSpecificationEqualityComparer(session);
                comparer.RegisterComparer((ControllUser x) => x.Id);
                comparer.RegisterComparer((ClientCommunicator x) => x.Id);
                comparer.RegisterComparer((Activity x) => x.Id);

                new PersistenceSpecification<ActivityInvocationQueueItem>(session, comparer)
                    .CheckProperty(x => x.Delivered, DateTime.Parse("2004-03-11 13:22:11"))
                    .CheckProperty(x => x.RecievedAtCloud, DateTime.Parse("2004-03-11 13:22:11"))
                    .CheckReference(x => x.Sender, user)
                    .CheckReference(x => x.Reciever, user)
                    .CheckReference(x => x.Activity, activity)
                    .CheckProperty(x => x.CommandName, "commandName")
                     // How to test dictionary persistence?
                    .VerifyTheMappings();
            }
        }
        
        [Test]
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
    }
}

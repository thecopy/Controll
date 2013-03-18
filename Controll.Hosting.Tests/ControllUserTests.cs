using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class ControllUserTests :TestBase
    {
        [TestMethod]
        public void ShouldCascadeWhenAddingUser()
        {
            using (var session = SessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var paramterDescriptors = Builder<ParameterDescriptor>.CreateListOfSize(2).Build();
                var commands = Builder<ActivityCommand>
                    .CreateListOfSize(2)
                    .All().Do(ac => ac.ParameterDescriptors = paramterDescriptors)
                    .Build();

                var activities = Builder<Activity>
                    .CreateListOfSize(2)
                    .All().Do(a => a.Commands = commands)
                    .Build();
                var zombies = Builder<Zombie>
                    .CreateListOfSize(2)
                    .All().Do(z => z.Activities = activities)
                    .Build();

                var userRepository = new ControllUserRepository(session);
                var user = new ControllUser
                    {
                        ConnectedClients = Builder<ControllClient>.CreateListOfSize(2).Build(),
                        Zombies = zombies,
                        EMail = "email",
                        Id = 3,
                        Password = "pass",
                        UserName = "userCascadingTest"
                    };

                userRepository.Add(user);
                var gotten = userRepository.GetByUserName("userCascadingTest");

                Assert.AreEqual(2, gotten.Zombies.Count);
                Assert.AreEqual(2, gotten.Zombies[0].Activities.Count);
                Assert.AreEqual(2, gotten.Zombies[0].Activities[0].Commands.Count);
                Assert.AreEqual(2, gotten.Zombies[0].Activities[0].Commands[0].ParameterDescriptors.Count);
                
                Assert.AreEqual(2, gotten.ConnectedClients.Count);

                transaction.Rollback();
            }
        }
    }
}

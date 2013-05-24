using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                var userRepository = new ControllUserRepository(session);
                var user = new ControllUser
                    {
                        Email = "email",
                        Id = 3,
                        Password = "pass",
                        UserName = "userCascadingTest"
                    };
                var clients = Builder<ControllClient>.CreateListOfSize(2).Build();
                foreach (var client in clients)
                    user.ConnectedClients.Add(client);

                userRepository.Add(user);
                var gotten = userRepository.GetByUserName("userCascadingTest");
                
                Assert.AreEqual(2, gotten.ConnectedClients.Count);

                transaction.Rollback();
            }
        }
    }
}

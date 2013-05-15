using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Controll.Hosting.Hubs;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Microsoft.AspNet.SignalR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class NinjectTests
    {
        [TestMethod]
        public void ShouldBeAbleToResolveHubs()
        {
            Bootstrapper.SetupNinject("testing");

            IDependencyResolver ninjectDependencyResolver = Bootstrapper.NinjectDependencyResolver;

            var zombieHub = ninjectDependencyResolver.Resolve<ZombieHub>();
            var clientHub = ninjectDependencyResolver.Resolve<ClientHub>();
            Assert.IsNotNull(zombieHub);
            Assert.IsNotNull(clientHub);
        }

        [TestMethod]
        public void ShouldBeAbleToInjectNewSessionInstancesIntoNewHubInstances()
        {
            Bootstrapper.SetupNinject("testing");

            IDependencyResolver ninjectDependencyResolver = Bootstrapper.NinjectDependencyResolver;

            const int range = 10;
            var sessions = new List<ClientHub>();
            Enumerable.Range(0, range).AsParallel().ForAll(_ =>
                {
                    sessions.Add(ninjectDependencyResolver.Resolve<ClientHub>());
                });

            Assert.AreEqual(range, sessions.Select(x => x.Session).Distinct(new SessionEqualityComparer()).Count());
            Assert.IsTrue(sessions.All(x => x.Session == x.MessageQueueService.Session));
        }

        private class SessionEqualityComparer : IEqualityComparer<ISession>
        {
            public bool Equals(ISession x, ISession y)
            {
                return x != null && y != null &&
                    x.GetSessionImplementation().SessionId.Equals(y.GetSessionImplementation().SessionId);
            }

            public int GetHashCode(ISession obj)
            {
                return obj.GetSessionImplementation().SessionId.GetHashCode();
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Controll.Hosting.Hubs;
using Controll.Hosting.Infrastructure;
using Microsoft.AspNet.SignalR;
using NUnit.Framework;
using NUnit.Framework;
using NHibernate;

namespace Controll.Hosting.Tests
{
    public class NinjectTests
    {
        [Test]
        public void ShouldBeAbleToResolveHubs()
        {
            Bootstrapper.Kernel = null;
            Bootstrapper.ApplyConfiguration(new BootstrapConfiguration
                {
                    ConnectionStringAlias = "testing"
                });

            var ninjectDependencyResolver = Bootstrapper.NinjectDependencyResolver;
            
            var zombieHub = ninjectDependencyResolver.GetService(typeof (ZombieHub));
            var clientHub = ninjectDependencyResolver.GetService(typeof (ClientHub));

            Assert.NotNull(clientHub);
            Assert.NotNull(zombieHub);
        }

        [Test]
        public void ShouldBeAbleToInjectNewSessionInstancesIntoNewHubInstances()
        {
            Bootstrapper.Kernel = null;
            Bootstrapper.ApplyConfiguration(new BootstrapConfiguration
            {
                ConnectionStringAlias = "testing"
            });

            IDependencyResolver ninjectDependencyResolver = Bootstrapper.NinjectDependencyResolver;

            const int range = 10;
            var sessions = new List<ClientHub>();
            Enumerable.Range(0, range).AsParallel().ForAll(_ =>
                {
                    sessions.Add(ninjectDependencyResolver.Resolve<ClientHub>());
                });

            Assert.AreEqual(range, sessions.Select(x => x.Session).Distinct(new SessionEqualityComparer()).Count());
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

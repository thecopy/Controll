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
            Bootstrapper.StrapTheBoot();
            Bootstrapper.Kernel.Rebind<ISession>()
                  .ToMethod(context => NHibernateHelper.GetSessionFactoryForTesting().OpenSession()) 
                  .InThreadScope();

            IDependencyResolver ninjectDependencyResolver = Bootstrapper.NinjectDependencyResolver;

            var zombieHub = ninjectDependencyResolver.Resolve<ZombieHub>();
            var clientHub = ninjectDependencyResolver.Resolve<ClientHub>();
            Assert.IsNotNull(zombieHub);
            Assert.IsNotNull(clientHub);
        }
    }
}

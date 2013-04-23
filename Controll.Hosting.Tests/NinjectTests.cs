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
        public void ShouldBeAbleToResolveZombieHub()
        {
            Bootstrapper.StrapTheBoot();
            Bootstrapper.Kernel.Rebind<ISession>()
                  .ToMethod(context => NHibernateHelper.GetSessionFactoryForTesting().OpenSession()) 
                  .InThreadScope();

            IDependencyResolver ninjectDependencyResolver = Bootstrapper.NinjectDependencyResolver;
            
            var zombieHub = ninjectDependencyResolver.Resolve<ZombieHub>();
            Assert.IsNotNull(zombieHub);
        }

        [TestMethod]
        public void ShouldBeAbleToResolveClientHub()
        {
            Bootstrapper.StrapTheBoot();
            Bootstrapper.Kernel.Rebind<ISession>()
                  .ToMethod(context => NHibernateHelper.GetSessionFactoryForTesting().OpenSession())
                  .InThreadScope();

            IDependencyResolver ninjectDependencyResolver = Bootstrapper.NinjectDependencyResolver;

            var zombieHub = ninjectDependencyResolver.Resolve<ClientHub>();
            Assert.IsNotNull(zombieHub);
        }
    }
}

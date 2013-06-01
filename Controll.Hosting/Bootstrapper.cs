using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using Controll.Hosting.Helpers;
using Controll.Hosting.Hubs;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Forms;
using NHibernate;
using Ninject;
using Ninject.Extensions.NamedScope;
using Owin;
using Owin.Types.Extensions;

namespace Controll.Hosting
{
    public static class Bootstrapper
    {
        private static IKernel _kernel;
        public static IKernel Kernel
        {
            get { return _kernel; }
            internal set { _kernel = value; }
        }

        public static NinjectDependencyResolver NinjectDependencyResolver { get; private set; }
        
        public static void ApplyConfiguration(ControllHostingConfiguration configuration)
        {
            if (!configuration.IsValid)
            {
                throw new InvalidOperationException("Configuration is not valid");
            }

            _kernel = new StandardKernel();

            if (configuration.UseCustomSessionFactory)
            {
                _kernel.Bind<ISessionFactory>()
                       .ToConstant(configuration.CustomSessionFactory)
                       .InSingletonScope();
            }
            else
            {
                _kernel.Bind<ISessionFactory>()
                       .ToMethod(_ => NHibernateHelper.GetSessionFactoryForConnectionStringAlias(configuration.ConnectionStringAlias, configuration.ClearDatabase))
                       .InSingletonScope();
            }

            _kernel.Bind<IConnectionManager>()
                  .ToMethod(_ => (IConnectionManager)NinjectDependencyResolver.GetFromBase<IConnectionManager>());

            _kernel.Bind<BaseHub>()
                   .ToSelf()
                   .DefinesNamedScope(configuration.HubScope);

            _kernel.Bind<ZombieHub>()
                   .ToSelf()
                   .DefinesNamedScope(configuration.HubScope);

            _kernel.Bind<ClientHub>()
                   .ToSelf()
                   .DefinesNamedScope(configuration.HubScope);

            _kernel.Bind<ISession>()
                   .ToMethod(x => x.Kernel.Get<ISessionFactory>().OpenSession())
                   .InNamedScope(configuration.HubScope);

            _kernel.Bind<IControllRepository>()
                  .To<ControllRepository>()
                  .InTransientScope();

            _kernel.Bind<IControllService>()
                  .To<ControllService>()
                  .InTransientScope();

            _kernel.Bind<IMembershipService>()
                  .To<MembershipService>()
                  .InTransientScope();

            NinjectDependencyResolver = new NinjectDependencyResolver(_kernel);
        }
    }
}

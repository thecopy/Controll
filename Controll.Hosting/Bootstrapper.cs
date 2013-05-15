using System;
using System.Diagnostics;
using Controll.Hosting.Hubs;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using NHibernate;
using Ninject;
using Ninject.Extensions.NamedScope;

namespace Controll.Hosting
{
    public static class Bootstrapper
    {
        public static IKernel Kernel
        {
            get { return _kernel; }
            private set { _kernel = value; }
        }

        public static NinjectDependencyResolver NinjectDependencyResolver { get; private set; }
        private static IKernel _kernel;

        public static void UseKernel(IKernel kernel)
        {
            if(_kernel != null)
                throw new InvalidOperationException("Cannot set kernel. It is already set!");
            _kernel = kernel;
        }

        public static void SetupNinject(string connectionStringAlias = "mocked")
        {
            const string hubScope = "Hub";
            if(Kernel == null)
            {
                _kernel = new StandardKernel();
            }

            // Do not use GlobalHost.ConnectionManager. It will try to resolve it with the DependecyResolver
            // which is THIS. GetFromBase uses the default SignalR resolver.
            _kernel.Bind<IConnectionManager>()
                  .ToMethod(_ => (IConnectionManager)NinjectDependencyResolver.GetFromBase<IConnectionManager>());

            _kernel.Bind<BaseHub>()
                   .ToSelf();

            _kernel.Bind<ZombieHub>()
                   .ToSelf()
                   .DefinesNamedScope(hubScope);

            _kernel.Bind<ClientHub>()
                   .ToSelf()
                   .DefinesNamedScope(hubScope);

            _kernel.Bind<ISession>()
                   .ToMethod(context =>
                       {
                           throw new StaleObjectStateException("Session", "a");
                           Debug.WriteLine("Getting new session!");
                           return NHibernateHelper.GetSessionFactoryForConnectionStringAlias(connectionStringAlias).OpenSession();
                       })
                   .InNamedScope(hubScope);

            _kernel.Bind(typeof(IGenericRepository<>))
                  .To(typeof(GenericRepository<>))
                  .InTransientScope();

            _kernel.Bind<IQueueItemRepostiory>()
                  .To<QueueItemRepostiory>()
                  .InTransientScope();

            _kernel.Bind<IControllUserRepository>()
                  .To<ControllUserRepository>()
                  .InTransientScope();

            _kernel.Bind<IMessageQueueService>()
                  .To<MessageQueueService>()
                  .InTransientScope();

            _kernel.Bind<IMembershipService>()
                  .To<MembershipService>()
                  .InTransientScope();

            _kernel.Bind<IActivityMessageLogService>()
                  .To<ActivityMessageLogService>()
                  .InTransientScope();

            NinjectDependencyResolver = new NinjectDependencyResolver(_kernel);
        }

        public static void SetupSessionPipelineInjector()
        {
        }
    }
}

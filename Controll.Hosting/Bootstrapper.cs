using System;
using Controll.Hosting.Hubs;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using NHibernate;
using Ninject;
using Ninject.Activation;

namespace Controll.Hosting
{
    public static class Bootstrapper
    {
        public static IKernel Kernel = null;
        public static NinjectDependencyResolver NinjectDependencyResolver = null;

        public static void StrapTheBoot(string connectionStringAlias = "mocked")
    {
        var kernel = new StandardKernel();

        // Do not use GlobalHost.ConnectionManager. It will try to resolve it with the DependecyResolver
        // which is THIS. GetFromBase uses the default resolver.
        kernel.Bind<IConnectionManager>()
              .ToMethod(_ => (IConnectionManager) NinjectDependencyResolver.GetFromBase<IConnectionManager>());

        kernel.Bind(typeof (IGenericRepository<>))
              .To(typeof (GenericRepository<>))
              .InThreadScope();

        kernel.Bind<IQueueItemRepostiory>()
              .To<QueueItemRepostiory>()
              .InThreadScope();

        kernel.Bind<IControllUserRepository>()
              .To<ControllUserRepository>()
              .InThreadScope();

        kernel.Bind<IMessageQueueService>()
              .To<MessageQueueService>()
              .InThreadScope();

        kernel.Bind<IActivityService>()
              .To<ActivityService>()
              .InThreadScope();

        kernel.Bind<ISession>()
              .ToMethod(context => NHibernateHelper.GetSessionFactoryForConnectionStringAlias(connectionStringAlias).OpenSession())
              .InThreadScope();

        Kernel = kernel;
        NinjectDependencyResolver = new NinjectDependencyResolver(kernel);
    }
    }
}

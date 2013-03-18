using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using NHibernate;
using Ninject;

namespace Controll.Hosting
{
    public static class Bootstrapper
    {
        public static IKernel Kernel = null;
        public static IDependencyResolver NinjectDependencyResolver = null;

        public static void StrapTheBoot()
        {
            var kernel = new StandardKernel();

            kernel.Bind(typeof (IGenericRepository<>))
                  .To(typeof (GenericRepository<>))
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
                  .ToMethod(context => NHibernateHelper.GetSessionFactoryForMockedData().OpenSession())
                  .InThreadScope();

            Kernel = kernel;
            NinjectDependencyResolver = new NinjectDependencyResolver(kernel);
        }
    }
}

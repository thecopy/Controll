using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Ninject;
using SignalR.Ninject;

namespace Controll.Hosting
{
    public static class Bootstrapper
    {
        internal static IKernel Kernel = null;
        internal static NinjectDependencyResolver NinjectDependencyResolver = null;
        public static void StrapTheBoot()
        {
            var kernel = new StandardKernel();

            kernel.Bind(typeof(IGenericRepository<>))
                .To(typeof(GenericRepository<>))
                .InRequestScope();

            kernel.Bind<IControllUserRepository>()
                .To<ControllUserRepository>()
                .InRequestScope();

            kernel.Bind<IMessageQueueService>()
                .To<MessageQueueService>()
                .InSingletonScope();

            kernel.Bind<IActivityService>()
                .To<ActivityService>()
                .InSingletonScope();
            
            Kernel = kernel;
            NinjectDependencyResolver = new NinjectDependencyResolver(kernel);
        }
    }
}

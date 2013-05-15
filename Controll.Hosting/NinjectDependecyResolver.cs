﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Ninject;

namespace Controll.Hosting
{
    public class NinjectDependencyResolver : DefaultDependencyResolver
    {
        private readonly IKernel _kernel;
        
        public object GetFromBase(Type type)
        {
            return base.GetService(type);
        }

        public object GetFromBase<T>()
        {
            return GetFromBase(typeof (T));
        }

        public NinjectDependencyResolver(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }

            _kernel = kernel;
        }

        public override object GetService(Type serviceType)
        {
            var result = _kernel.TryGet(serviceType) ?? base.GetService(serviceType);

            return result;
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            return _kernel.GetAll(serviceType).Concat(base.GetServices(serviceType));
        }
    }
}

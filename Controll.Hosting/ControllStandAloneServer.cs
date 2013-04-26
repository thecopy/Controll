using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Ninject;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;

namespace Controll.Hosting 
{
    public class ControllStandAloneServer
    {
        private readonly string _url;
        public ControllStandAloneServer(string url)
        {
            _url = url;
            Bootstrapper.StrapTheBoot();
            GlobalHost.DependencyResolver = Bootstrapper.NinjectDependencyResolver;

        }

        public IDisposable Start()
        {
            return WebApplication.Start<Startup>(_url);
        }

        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                // Turn cross domain on 
                var config = new HubConfiguration {EnableDetailedErrors = true, EnableCrossDomain = true, Resolver = Bootstrapper.NinjectDependencyResolver };

                // This will map out to http://localhost:8080/signalr by default
                
                app.MapHubs(config);
            }
        }
    }
}
 
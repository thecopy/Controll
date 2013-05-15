﻿using System;
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
            Bootstrapper.SetupNinject();
            Bootstrapper.SetupSessionPipelineInjector(); 
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
                var config = new HubConfiguration
                    {
                        EnableDetailedErrors = true, 
                        EnableCrossDomain = true, 
                        Resolver = Bootstrapper.NinjectDependencyResolver
                    };

                // This will map out to http://localhost:8080/signalr by default
                app.MapHubs(config);
            }
        }
    }
}
 
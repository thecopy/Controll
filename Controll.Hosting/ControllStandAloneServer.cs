using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using Controll.Hosting.Helpers;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Forms;
using NHibernate;
using Owin;
using Owin.Types.Extensions;

namespace Controll.Hosting
{
    public class ControllStandAloneServer
    {
        private readonly string _url;

        private ControllHostingConfiguration _configuration;

        public ControllStandAloneServer UseBootstrapConfiguration(ControllHostingConfiguration configuration)
        {
            _configuration = configuration;

            return this;
        }

        public ControllStandAloneServer(string url)
        {
            _url = url;

            _configuration = new ControllHostingConfiguration
                {
                    ClearDatabase = false,
                    ConnectionStringAlias = "mocked",
                    UseCustomSessionFactory = false
                };
        }

        public IDisposable Start()
        {
            Bootstrapper.ApplyConfiguration(_configuration);

            GlobalHost.DependencyResolver = Bootstrapper.NinjectDependencyResolver;

            return WebApp.Start<Startup>(_url);
        }

        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                // This will map out to http://localhost:8080/signalr by default
                // Turn cross domain on 
                var config = new HubConfiguration
                    {
                        EnableDetailedErrors = true,
                        EnableCrossDomain = true,
                        Resolver = Bootstrapper.NinjectDependencyResolver
                    };

                app.UseControllAuth(Bootstrapper.NinjectDependencyResolver.Resolve<ISessionFactory>());
                app.MapHubs(config);

                Sweep();
            }

            // Clears all connected clients
            private static void Sweep()
            {
                using (var session = Bootstrapper.NinjectDependencyResolver.Resolve<ISessionFactory>().OpenSession())
                using (var tx = session.BeginTransaction())
                {
                    session.CreateQuery("DELETE FROM ControllClient").ExecuteUpdate();
                    tx.Commit();
                }
            }
        }
    }
}
 
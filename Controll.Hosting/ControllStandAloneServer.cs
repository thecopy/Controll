using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Forms;
using NHibernate;
using Owin;
using Owin.Types;
using Owin.Types.Extensions;
using Controll.Hosting.Infrastructure;

namespace Controll.Hosting 
{
    public class ControllStandAloneServer
    {
        private readonly string _url;

        private BootstrapConfiguration _configuration;

        public ControllStandAloneServer UseBootstrapConfiguration(BootstrapConfiguration configuration)
        {
            _configuration = configuration;

            return this;
        }

        public ControllStandAloneServer(string url)
        {
            _url = url;

            _configuration = new BootstrapConfiguration
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

                SetupAuth(app);
                app.MapHubs(config);

            }

            // Used for auth
            private static ISessionFactory _sessionFactory;
            private void SetupAuth(IAppBuilder app)
            {
                var options = new FormsAuthenticationOptions
                {
                    AuthenticationMode = AuthenticationMode.Active,
                    AuthenticationType = Constants.ControllAuthType,
                    CookieHttpOnly = true,
                    CookieName = "controll.auth.id",
                    CookiePath = "/",
                    CookieSecure = CookieSecureOption.Never,
                    ExpireTimeSpan = TimeSpan.FromMinutes(10),
                    ReturnUrlParameter = "ReturnUrl",
                    SlidingExpiration = true,
                    Provider = new FormsAuthenticationProvider()
                };
                app.SetDataProtectionProvider(new DpapiDataProtectionProvider());
                app.UseFormsAuthentication(options);
                app.MapPath("/auth", builder => builder.UseHandler((req, res) =>
                    {
                        res.AddHeader("Cache-Control", "no-cache");

                        if (req.Method == "POST")
                        {
                            if (_sessionFactory == null)
                                _sessionFactory = Bootstrapper.NinjectDependencyResolver.Resolve<ISessionFactory>();

                            res.StatusCode = (int) HttpStatusCode.Forbidden;
                            res.ReasonPhrase = "Authentication Failed";

                            ControllAuthentication.AuthenticateForms(req, res, _sessionFactory.OpenSession());
                        }
                        else
                        {
                            res.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            res.ReasonPhrase = "Use POST";
                        }
                    }));
                //app.UseDenyAnonymous();
                app.MapPath("/authed", builder => builder.UseHandler((req, res) =>
                    {
                        var claimsPrincipal = req.User as ClaimsPrincipal;
                        byte[] body = claimsPrincipal == null 
                                          ? Encoding.ASCII.GetBytes("Somehow your IPrincipal is null!???!")
                                          : Encoding.ASCII.GetBytes("You can apparently be here, so you must be authed since this is after \"UseDenyAnonymous\" :)\nClaims: " + req.User.Identity);

                        res.Body.Write(body, 0, body.Length);
                        res.StatusCode = 200;
                        res.ReasonPhrase = "OK";
                    }));
            }
        }
    }
}
 
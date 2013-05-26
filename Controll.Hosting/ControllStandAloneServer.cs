using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using Controll.Hosting.Helpers;
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
            private static readonly ISessionFactory SessionFactory = Bootstrapper.NinjectDependencyResolver.Resolve<ISessionFactory>();
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
                            var body = new StreamReader(req.Body).ReadToEnd();

                            var username = RequestHelper.GetBodyRequestPart(body, "username");
                            var pass = RequestHelper.GetBodyRequestPart(body, "password");
                            var zombie = RequestHelper.GetBodyRequestPart(body, "zombie");
                            
                            try
                            {
                                using (var session = SessionFactory.OpenSession())
                                {
                                    var controllRepository = new ControllRepository(session);
                                    var membershipService = new MembershipService(session, controllRepository);

                                    var identity = ControllAuthentication.AuthenticateForms(username, pass, zombie, membershipService);
                                    res.SignIn(new ClaimsPrincipal(identity));
                                    res.StatusCode = (int) HttpStatusCode.NoContent;
                                }
                            }
                            catch (InvalidOperationException ex)
                            {
                                res.StatusCode = (int)HttpStatusCode.Forbidden;
                                res.ReasonPhrase = "Authentication Failed: " + ex.Message;
                            }
                        }
                        else
                        {
                            res.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        }
                    }));

                app.MapPath("/registeruser", builder => builder.UseHandler((req, res) =>
                {
                    res.AddHeader("Cache-Control", "no-cache");

                    if (req.Method == "POST")
                        {
                            var body = new StreamReader(req.Body).ReadToEnd();

                            var username = RequestHelper.GetBodyRequestPart(body, "username");
                            var pass = RequestHelper.GetBodyRequestPart(body, "password");
                            var email = RequestHelper.GetBodyRequestPart(body, "email");
                            
                            try
                            {
                                using (var session = SessionFactory.OpenSession())
                                {
                                    var controllRepository = new ControllRepository(session);
                                    var membershipService = new MembershipService(session, controllRepository);

                                    membershipService.AddUser(username, pass, email);
                                    res.StatusCode = (int) HttpStatusCode.NoContent;
                                }
                            }
                            catch (InvalidOperationException ex)
                            {
                                res.StatusCode = (int)HttpStatusCode.Forbidden;
                                res.ReasonPhrase = "Registration Failed: " + ex.Message;
                            }
                        }
                        else
                        {
                            res.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        }
                }));

                app.UseDenyAnonymous();
            }
        }
    }
}
 
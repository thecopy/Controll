using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Forms;
using NHibernate;
using Owin;
using Owin.Types.Extensions;

namespace Controll.Hosting.Helpers
{
    public static class AppBuilderExtension
    {
        public static IAppBuilder UseControll(this IAppBuilder app, ControllHostingConfiguration configuration)
        {
            Bootstrapper.ApplyConfiguration(configuration);

            var sessionFactory = (ISessionFactory) Bootstrapper.NinjectDependencyResolver.GetService(typeof (ISessionFactory));
            app.UseControllAuth(sessionFactory, configuration.LoginRoute);

            if (configuration.DenyAnonymous)
                app.UseDenyAnonymous();

            configuration.HubConfiguration.Resolver = Bootstrapper.NinjectDependencyResolver;
            app.MapHubs(configuration.HubConfiguration);

            return app;
        }

        public static IAppBuilder UseControllAuth(this IAppBuilder app, ISessionFactory sessionFactory, string loginRoute = null)
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
                LoginPath = loginRoute,
                SlidingExpiration = true,
                Provider = new FormsAuthenticationProvider()
            };
            app.SetDataProtectionProvider(new DpapiDataProtectionProvider((string)app.Properties["host.AppName"]));
            app.UseFormsAuthentication(options);

            app.MapPath("/auth", builder => builder.UseHandler((req, res) =>
            {
                res.AddHeader("Cache-Control", "no-cache");
                if (req.Method != "POST")
                {
                    res.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return;
                }

                var body = new StreamReader(req.Body).ReadToEnd();

                var username = RequestHelper.GetRequestBodyPart(body, "username");
                var pass = RequestHelper.GetRequestBodyPart(body, "password");
                var zombie = RequestHelper.GetRequestBodyPart(body, "zombie");

                try
                {
                    using (var session = sessionFactory.OpenSession())
                    using (var tx = session.BeginTransaction())
                    {
                        var controllRepository = new ControllRepository(session);
                        var membershipService = new MembershipService(session, controllRepository);

                        var identity = ControllAuthentication.AuthenticateForms(username, pass, zombie, membershipService);
                        res.SignIn(new ClaimsPrincipal(identity));
                        res.StatusCode = (int)HttpStatusCode.NoContent;

                        tx.Commit();
                    }
                }
                catch (InvalidOperationException ex)
                {
                    res.StatusCode = (int)HttpStatusCode.Forbidden;
                    res.ReasonPhrase = "Authentication Failed: " + ex.Message;
                }
            }));

            app.MapPath("/registeruser", builder => builder.UseHandler((req, res) =>
            {
                res.AddHeader("Cache-Control", "no-cache");
                if (req.Method != "POST")
                {
                    res.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return;
                }

                var body = new StreamReader(req.Body).ReadToEnd();

                var username = RequestHelper.GetRequestBodyPart(body, "username");
                var pass = RequestHelper.GetRequestBodyPart(body, "password");
                var email = RequestHelper.GetRequestBodyPart(body, "email");

                try
                {
                    using (var session = sessionFactory.OpenSession())
                    using (var tx = session.BeginTransaction())
                    {
                        var controllRepository = new ControllRepository(session);
                        var membershipService = new MembershipService(session, controllRepository);

                        membershipService.AddUser(username, pass, email);
                        res.StatusCode = (int)HttpStatusCode.NoContent;

                        tx.Commit();
                    }
                }
                catch (InvalidOperationException ex)
                {
                    res.StatusCode = (int)HttpStatusCode.Conflict;
                    res.ReasonPhrase = ex.Message;
                }
            }));

            return app;
        }
    }
}

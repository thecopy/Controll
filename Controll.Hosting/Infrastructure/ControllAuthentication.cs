using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using NHibernate;
using Owin;
using Owin.Types;
using Owin.Types.Extensions;

namespace Controll.Hosting.Infrastructure
{
    public static class ControllAuthentication
    {
        public static void AuthenticateForms(OwinRequest req, OwinResponse res, ISession session)
        {
            var body = new StreamReader(req.Body).ReadToEnd();
            var parts = body.Split(new[] {'&'});

            if (!(parts[0].StartsWith("username=") && parts[1].StartsWith("password=")) 
                || (parts.Length == 3 && !(parts[2].StartsWith("zombie="))))
            {
                return;
            }

            var username = parts[0].Substring(9);
            var pass = parts[1].Substring(9);

            string zombie = null;
            if (parts.Length == 3)
                zombie = parts[2].Substring(7);
            
            ControllUser user;
            try
            {
                // Perform check on username and password
                var membershipService = new MembershipService(new ControllUserRepository(session));
                user = membershipService.AuthenticateUser(username, pass);

                if (zombie != null && user.GetZombieByName(zombie) == null)
                {
                    res.ReasonPhrase = "Zombie Not Found";
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                return;
            }

            var claims = new List<Claim>();

            claims.Add(new Claim(ControllClaimTypes.UserIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)));
            if (zombie != null)
                claims.Add(new Claim(ControllClaimTypes.ZombieIdentifier, user.GetZombieByName(zombie).Id.ToString(CultureInfo.InvariantCulture)));

            var identity = new ClaimsIdentity(claims, Constants.ControllAuthType);
            res.SignIn(new ClaimsPrincipal(identity));

            res.StatusCode = (int) HttpStatusCode.NoContent;
            res.ReasonPhrase = "Authentication Successfull";
        }

        public static void RegisterUser(OwinRequest req, OwinResponse res, ISession session)
        {
            throw new NotImplementedException();
        }
    }
}

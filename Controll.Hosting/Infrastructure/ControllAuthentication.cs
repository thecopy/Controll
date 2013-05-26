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
        public static ClaimsIdentity AuthenticateForms(string username, string pass, string zombie, IMembershipService membershipService)
        {
            var user = membershipService.AuthenticateUser(username, pass);

            if (zombie != null && user.GetZombieByName(zombie) == null)
            {
                throw new InvalidOperationException("Zombie not found: " + zombie);
            }

            var claims = new List<Claim>();

            claims.Add(new Claim(ControllClaimTypes.UserIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)));

            if (zombie != null)
                claims.Add(new Claim(ControllClaimTypes.ZombieIdentifier, user.GetZombieByName(zombie).Id.ToString(CultureInfo.InvariantCulture)));

            return new ClaimsIdentity(claims, Constants.ControllAuthType);
        }
    }
}

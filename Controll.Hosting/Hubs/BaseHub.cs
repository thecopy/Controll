using System;
using System.Security.Claims;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Microsoft.AspNet.SignalR;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    public class BaseHub : Hub
    {
        public ISession Session { get; private set; }

        public BaseHub(ISession session)
        {
            Session = session;
        }

        protected ControllUser GetUser()
        {
            var userPrincial = Context.User as ClaimsPrincipal;
            int id = int.Parse(userPrincial.GetClaim(ControllClaimTypes.UserIdentifier));

            var user =  Session.Get<ControllUser>(id);
            if(user == null)
                throw new InvalidOperationException("Did not find user with id " + userPrincial.GetClaim(ControllClaimTypes.UserIdentifier));

            return user;
        }

        protected Zombie GetZombie()
        {
            var userPrincial = Context.User as ClaimsPrincipal;
            int id = int.Parse(userPrincial.GetClaim(ControllClaimTypes.ZombieIdentifier));

            var zombie = Session.Get<Zombie>(id);
            if (zombie == null)
                throw new InvalidOperationException("Did not find zombie with id " + userPrincial.GetClaim(ControllClaimTypes.ZombieIdentifier));

            return zombie;
        }
    }
}

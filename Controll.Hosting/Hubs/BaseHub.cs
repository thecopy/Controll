using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    public class BaseHub : Hub
    {
        protected readonly IControllRepository ControllRepository;
        protected readonly IControllService ControllService;
        internal ISession Session { get; private set; }

        public BaseHub(ISession session, IControllRepository controllRepository, IControllService controllService)
        {
            ControllRepository = controllRepository;
            ControllService = controllService;
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

        public override Task OnDisconnected()
        {
            using (var transaction = Session.BeginTransaction())
            {
                var client = ControllRepository.GetClientByConnectionId(Context.ConnectionId);

                if (client != null)
                {
                    Session.Delete(client);
                }
                transaction.Commit();
            }

            return null;
        }
    }
}

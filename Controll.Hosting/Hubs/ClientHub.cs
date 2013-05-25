using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    [AuthorizeClaim(ControllClaimTypes.UserIdentifier)]
    public class ClientHub : BaseHub
    {
        private readonly IMembershipService _membershipService;
        private readonly IMessageQueueService _messageQueueService;

        public ClientHub(IControllRepository controllRepository,
                         IMembershipService membershipService,
                         IMessageQueueService messageQueueService,
                         ISession session)
            : base(session, controllRepository)
        {
            _membershipService = membershipService;
            _messageQueueService = messageQueueService;
        }

        public void SignIn()
        {
            using (var transaction = Session.BeginTransaction())
            {
                var user = GetUser();

                var client = new ControllClient
                    {
                        ConnectionId = Context.ConnectionId,
                        ClientCommunicator = user
                    };

                Session.Save(client);
                transaction.Commit();
            }
        }

        public IEnumerable<ZombieViewModel> GetAllZombies()
        {
            using (var tx = Session.BeginTransaction())
            {
                var user = GetUser();
                Console.WriteLine(user.UserName + " is fetching all zombies");

                // ToList() -> Enumerate it so that the NHibernate proxy will load in this thread
                // and not in SignalR's serializer
                var vms = user.Zombies.Select(ViewModelHelper.CreateViewModel).ToList(); 
                tx.Commit();

                return vms;
            }
        }

        public bool RegisterUser(string userName, string password, string email)
        {
            using (var transaction = Session.BeginTransaction())
            {
                _membershipService.AddUser(userName, password, email);
                transaction.Commit();
            }
            return true;
        }


        public bool IsZombieOnline(string zombieName)
        {
            using (var transaction = Session.BeginTransaction())
            {
                var user = GetUser();
                var zombie = user.GetZombieByName(zombieName);

                if (zombie == null)
                    throw new ArgumentException("Zombie does not exist", "zombieName");

                Console.WriteLine("Checking online status for zombie " + zombieName + " for user " + user.UserName);

                NHibernateUtil.Initialize(zombie.ConnectedClients);
                transaction.Commit();

                return zombie.IsOnline();
            }
        }

        public Guid StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters, string commandName)
        {
            using (var transaction = Session.BeginTransaction())
            {
                var user = GetUser();

                Console.WriteLine("User '{0}' is requesting to start activity with key {1}", user.UserName, activityKey);

                var zombie = user.GetZombieByName(zombieName);
                if (zombie == null)
                    throw new Exception("Zombie not found");

                var activity = zombie.GetActivity(activityKey);
                if (activity == null)
                    throw new Exception("Activity not found. Searched for activity with key " + activityKey + ". Zombie has " + zombie.Activities.Count + " installed activities");

                var queueItem = _messageQueueService.InsertActivityInvocation(zombie, activity, parameters, commandName, Context.ConnectionId);
                transaction.Commit();

                Console.WriteLine("Queueing activity " + activity.Name + " on zombie " + zombie.Name);
                _messageQueueService.ProcessQueueItem(queueItem);
                return queueItem.Ticket;
            }
        }

        public Guid PingZombie(string zombieName)
        {
            using (var transaction = Session.BeginTransaction())
            {
                var zombie = GetUser().GetZombieByName(zombieName);

                if (zombie == null)
                    throw new Exception("Could not find zombie " + zombieName);

                var queueItem = _messageQueueService.InsertPingMessage(zombie, Context.ConnectionId);
                transaction.Commit();

                _messageQueueService.ProcessQueueItem(queueItem);
                return queueItem.Ticket;
            }
        }
    }
}

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
using NHibernate.Proxy;

namespace Controll.Hosting.Hubs
{
    [AuthorizeClaim(ControllClaimTypes.UserIdentifier)]
    public class ClientHub : BaseHub
    {
        public ClientHub(IControllRepository controllRepository,
                         IControllService controllService,
                         ISession session)
            : base(session, controllRepository, controllService)
        {}

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
            Console.WriteLine("User logged in with connection id" + Context.ConnectionId);
        }

        // ReSharper disable SuspiciousTypeConversion.Global
        public IEnumerable<ZombieViewModel> GetAllZombies()
        {
            using (var tx = Session.BeginTransaction())
            {
                var user = GetUser();
                Console.WriteLine(user.UserName + " is fetching all zombies");

                if (user.Zombies is INHibernateProxy)
                    Session.GetSessionImplementation().PersistenceContext.Unproxy(user.Zombies);

                var vms = user.Zombies.Select(ViewModelHelper.CreateViewModel).ToList(); 

                tx.Commit();
                return vms;
            }
        }
        // ReSharper restore SuspiciousTypeConversion.Global
        
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

                var queueItem = ControllService.InsertActivityInvocation(zombie, activity, parameters, commandName, Context.ConnectionId);
                
                transaction.Commit();

                Console.WriteLine("Queueing activity " + activity.Name + " on zombie " + zombie.Name);
                ControllService.ProcessQueueItem(queueItem);

                return queueItem.Ticket;
            }
        }

        public void AddZombie(string zombieName)
        {
            using (var transaction = Session.BeginTransaction())
            {
                var user = GetUser();

                 if (user.GetZombieByName(zombieName) != null)
                    throw new InvalidOperationException(
                        String.Format("A zombie with name {0} already exists for user {1}", zombieName, user.UserName));

                var zombie = new Zombie
                {
                    Owner = user,
                    Name = zombieName
                };

                Session.Save(zombie);
                transaction.Commit();
            }
        }

        public Guid PingZombie(string zombieName)
        {
            using (var transaction = Session.BeginTransaction())
            {
                var zombie = GetUser().GetZombieByName(zombieName);

                if (zombie == null)
                    throw new Exception("Could not find zombie " + zombieName);

                var queueItem = ControllService.InsertPingMessage(zombie, Context.ConnectionId);
                transaction.Commit();

                ControllService.ProcessQueueItem(queueItem);
                return queueItem.Ticket;
            }
        }

        public IEnumerable<LogBookViewModel> GetLogBooks(int take, int skip)
        {
            if(take > 50)
                throw new InvalidOperationException("Cannot take more than 50");

            using (var transaction = Session.BeginTransaction())
            {
                var user = GetUser();

                if (user.LogBooks is INHibernateProxy)
                    Session.GetSessionImplementation().PersistenceContext.Unproxy(user.Zombies);

                var books = user.LogBooks.Skip(skip).Take(take).Select(x => new LogBookViewModel
                    {
                        ActivityName = x.Activity.Name,
                        CommandLabel = x.CommandName,
                        Delivered = x.Started,
                        InvocationTicket = x.InvocationTicket,
                        Parameters = x.Parameters,
                        Messages = x.LogMessages.Select(y => new LogMessageViewModel
                            {
                                Date = y.Date,
                                Message = y.Message,
                                MessageType = y.Type
                            })
                    });

                transaction.Commit();
                return books;
            }
        } 
    }
}

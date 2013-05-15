﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Microsoft.AspNet.SignalR;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    public class ClientHub : BaseHub
    {
        private readonly IControllUserRepository _controllUserRepository;
        private readonly IMembershipService _membershipService;
        private readonly IMessageQueueService _messageQueueService;

        public ClientHub(IControllUserRepository controllUserRepository,
                         IMembershipService membershipService,
                         IMessageQueueService messageQueueService,
                         ISession session) : base(session)
        {
            _controllUserRepository = controllUserRepository;
            _membershipService = membershipService;
            _messageQueueService = messageQueueService;
        }

        private ControllUser GetUser()
        {
            var userName = (string) Clients.Caller.userName;

            var user = _controllUserRepository.GetByUserName(userName);

            return user;
        }

        public bool LogOn(string password)
        {
            string userName = Clients.Caller.userName;
            var user = _membershipService.AuthenticateUser(userName, password);

            var client = new ControllClient
                {
                    ConnectionId = Context.ConnectionId,
                    ClientCommunicator = user
                };

            user.ConnectedClients.Add(client);

            using (var transaction = Session.BeginTransaction())
            {
                _controllUserRepository.Update(user);
                transaction.Commit();
            }

            return true;
        }

        [RequiresAuthorization]
        public IEnumerable<ZombieViewModel> GetAllZombies()
        {
            if (!EnsureUserIsLoggedIn())
                throw new Exception("Not authed");

            var user = GetUser();
            Console.WriteLine(user.UserName + " is fetching all zombies");

            return user.Zombies.Select(ViewModelHelper.CreateViewModel);
        }

        [RequiresAuthorization]
        public IEnumerable<ActivityViewModel> GetActivitesInstalledOnZombie(string zombieName)
        {
            if (!EnsureUserIsLoggedIn())
                throw new Exception("Not authed");

            var user = GetUser();

            return user.GetZombieByName(zombieName).Activities.Select(ViewModelHelper.CreateViewModel);
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


        [RequiresAuthorization]
        public bool IsZombieOnline(string zombieName)
        {
            if (!EnsureUserIsLoggedIn())
                throw new Exception("Not authed");

            var user = GetUser();
            var zombie = user.GetZombieByName(zombieName);

            if (zombie == null)
                throw new ArgumentException("Zombie does not exist", "zombieName");

            Console.WriteLine("Checking online status for zombie " + zombieName + " for user " + user.UserName);

            return zombie.IsOnline();
        }

        [RequiresAuthorization]
        public Guid StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters, string commandName)
        {
            if (!EnsureUserIsLoggedIn())
                throw new Exception("Not authenticated");

            var user = GetUser();

            Console.WriteLine("User '{0}' is requesting to start activity with key {1}", user.UserName, activityKey);

            var zombie = user.GetZombieByName(zombieName);
            if (zombie == null)
                throw new Exception("Zombie not found");

            var activity = zombie.GetActivity(activityKey);
            if (activity == null)
                throw new Exception("Activity not found. Searched for activity with key " + activityKey + ". Zombie has " + zombie.Activities.Count + " installed activities");

            using (var transaction = Session.BeginTransaction())
            {
                var queueItem = MessageQueueService.InsertActivityInvocation(zombie, activity, parameters, commandName, Context.ConnectionId);
                transaction.Commit();

                Console.WriteLine("Queueing activity " + activity.Name + " on zombie " + zombie.Name);
                MessageQueueService.ProcessQueueItem(queueItem);
                return queueItem.Ticket;
            }
        }

        [RequiresAuthorization]
        public Guid PingZombie(string zombieName)
        {
            if (!EnsureUserIsLoggedIn())
                throw new Exception("Not authed");

            var zombie = GetUser().GetZombieByName(zombieName);

            if (zombie == null)
                return default(Guid);

            using(var transaction = Session.BeginTransaction())
            {
                var queueItem = MessageQueueService.InsertPingMessage(zombie, Context.ConnectionId);
                transaction.Commit();

                MessageQueueService.ProcessQueueItem(queueItem);
                return queueItem.Ticket;
            }
        }

        private bool EnsureUserIsLoggedIn()
        {
            var claimedUserName = (string) Clients.Caller.userName;

            var user = _controllUserRepository.GetByConnectionId(Context.ConnectionId);

            if (user == null || user.UserName.ToLower() != claimedUserName.ToLower())
            {
                Console.WriteLine("User is not authenticated. Claimed username: \"" + claimedUserName + "\"");
                return false;
            }

            return true;
        }

        public override Task OnDisconnected()
        {
            var user = _controllUserRepository.GetByConnectionId(Context.ConnectionId);
            if (user == null) return null;
            var client = user.ConnectedClients.SingleOrDefault(z => z.ConnectionId == Context.ConnectionId);

            Console.Write("One of " + user.UserName + "'s clients disconnected");

            if (client != null)
            {
                using (var transaction = Session.BeginTransaction())
                {
                    user.ConnectedClients.Remove(client);
                    _controllUserRepository.Update(user);

                    transaction.Commit();
                }
            }


            return null;
        }
    }
}

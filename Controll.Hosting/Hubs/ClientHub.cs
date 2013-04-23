﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    public class ClientHub : BaseHub
    {
        private readonly IControllUserRepository _controllUserRepository;
        private readonly IMessageQueueService _messageQueueService;

        public ClientHub(IControllUserRepository controllUserRepository,
                         IMessageQueueService messageQueueService,
                         ISession session) : base(session)
        {
            _controllUserRepository = controllUserRepository;
            _messageQueueService = messageQueueService;
        }

        private ControllUser GetUser()
        {
            var userName = (string) Clients.Caller.UserName;

            ControllUser user = _controllUserRepository.GetByUserName(userName);

            return user;
        }

        public bool LogOn(string password)
        {
            Console.Write("Client trying to logon ");

            var user = GetUser();

            if (user == null)
                return false;

            Console.WriteLine("user: '" + user.UserName + "'");

            if (user.Password != password)
                return false;

            var client = new ControllClient
                {
                    ConnectionId = Context.ConnectionId,
                    DeviceType = DeviceType.Client
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
                return Enumerable.Empty<ZombieViewModel>();

            var user = GetUser();
            Console.WriteLine(user.UserName + " is fetching all zombies");

            return user.Zombies.Select(ViewModelHelper.CreateViewModel);
        }

        [RequiresAuthorization]
        public IEnumerable<ActivityViewModel> GetActivitesInstalledOnZombie(string zombieName)
        {
            if (!EnsureUserIsLoggedIn())
                return Enumerable.Empty<ActivityViewModel>();

            var user = GetUser();

            return user.GetZombieByName(zombieName).Activities.Select(ViewModelHelper.CreateViewModel);
        }

        public bool RegisterUser(string userName, string password, string email)
        {
            if (_controllUserRepository.GetByUserName(userName) != null
                || _controllUserRepository.GetByEMail(email) != null)
                return false;

            var newUser = new ControllUser
                {
                    EMail = email,
                    UserName = userName,
                    Password = password
                };

            using (var transaction = Session.BeginTransaction())
            {
                _controllUserRepository.Add(newUser);
                transaction.Commit();
            }
            return true;
        }


        [RequiresAuthorization]
        public bool IsZombieOnline(string zombieName)
        {
            if (!EnsureUserIsLoggedIn())
                return false;

            var user = GetUser();
            var zombie = user.GetZombieByName(zombieName);

            if (zombie == null)
                throw new ArgumentException("Zombie does not exist", "zombieName");

            Console.WriteLine("Checking online status for zombie " + zombieName + " for user " + user.UserName);

            return zombie.IsOnline();
        }

        [RequiresAuthorization]
        public Guid StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters)
        {
            if (!EnsureUserIsLoggedIn())
                return default(Guid);

            var user = GetUser();

            Console.WriteLine("User '{0}' is requesting to start activity", user.UserName);

            var zombie = user.GetZombieByName(zombieName);
            if (zombie == null)
                return default(Guid);

            var activity = zombie.GetActivity(activityKey);
            if (activity == null)
                return default(Guid);

            using (ITransaction transaction = Session.BeginTransaction())
            {
                var ticket = _messageQueueService.InsertActivityInvocation(zombie, activity, parameters, Context.ConnectionId);
                transaction.Commit();

                Console.WriteLine("Queueing activity " + activity.Name + " on zombie " + zombie.Name);
                return ticket;
            }
        }

        [RequiresAuthorization]
        public Guid PingZombie(string zombieName)
        {
            if (!EnsureUserIsLoggedIn())
                return default(Guid);

            var zombie = GetUser().GetZombieByName(zombieName);

            if (zombie == null)
                return default(Guid);

            using(var transaction = Session.BeginTransaction())
            {
                var ticket = _messageQueueService.InsertPingMessage(zombie, Context.ConnectionId);
                transaction.Commit();
                
                return ticket;
            }
        }

        private bool EnsureUserIsLoggedIn()
        {
            var claimedUserName = (string) Clients.Caller.UserName;

            var user = _controllUserRepository.GetByConnectionId(Context.ConnectionId);

            if (user == null || user.UserName.ToLower() != claimedUserName.ToLower())
            {
                Console.WriteLine("User is not authenticated. Claimed username: \"" + claimedUserName + "\"");
                return false;
            }

            return true;
        }

        public Task OnDisconnect()
        {
            var user = _controllUserRepository.GetByConnectionId(Context.ConnectionId);
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


        [ExcludeFromCodeCoverage]
        public Task OnConnect()
        {
            Console.WriteLine("Client connected");
            return null;
        }

        [ExcludeFromCodeCoverage]
        public Task OnReconnect(IEnumerable<string> groups)
        {
            Console.WriteLine("Reconnected");
            return null;
        }
    }
}

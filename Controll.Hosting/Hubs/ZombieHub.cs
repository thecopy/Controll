using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    public class ZombieHub : BaseHub
    {
        private readonly IControllUserRepository _controllUserRepository;
        private IActivityService _activityService;
        private IGenericRepository<Activity> _genericRepository;
        private readonly IMessageQueueService _messageQueueService;

        public ZombieHub(IControllUserRepository controllUserRepository,
                         IActivityService activityService,
                         IGenericRepository<Activity> genericRepository,
                         IMessageQueueService messageQueueService,
                         ISession session) : base(session)
        {
            _controllUserRepository = controllUserRepository;
            _activityService = activityService;
            _genericRepository = genericRepository;
            _messageQueueService = messageQueueService;
        }

        private ZombieState GetZombieState()
        {
            // TODO: Gör UserName:string -> User:ControllUser
            var state = new ZombieState
                {
                    Name = (string) Clients.Caller.ZombieName,
                    UserName = (string) Clients.Caller.BelongsToUser
                };

            return state;
        }

        private bool EnsureZombieAuthentication()
        {
            var claimedBelongingToUserName = (string) Clients.Caller.BelongsToUser;
            var claimedZombieName = (string) Clients.Caller.ZombieName;

            var user = _controllUserRepository.GetByUserName(claimedBelongingToUserName);

            if (user == null
                || user.UserName.ToLower() != claimedBelongingToUserName.ToLower()
                ||
                user.Zombies.SingleOrDefault(z => z.Name == claimedZombieName && z.ConnectionId == Context.ConnectionId) ==
                null)
            {
                return false;
            }

            return true;
        }

        public bool LogOn(string usernName, string password, string zombieName)
        {
            var user = _controllUserRepository.GetByUserName(usernName);

            if (user == null)
                return false;

            if (user.Password != password)
                return false;

            var zombie = user.GetZombieByName(zombieName);
            if (zombie == null)
                return false;

            zombie.ConnectionId = Context.ConnectionId;

            using (var transaction = Session.BeginTransaction())
            {
                _controllUserRepository.Update(user);
                transaction.Commit();
            }
            
            using (var transaction = Session.BeginTransaction())
            {
                _messageQueueService.ProcessUndeliveredMessagesForZombie(zombie);
                transaction.Commit();
            }

            return true;
        }

        public bool QueueItemDelivered(Guid ticket)
        {
            if (!EnsureZombieAuthentication())
                return false;

            using (var transaction = Session.BeginTransaction())
            {
                Console.WriteLine("A Zombie confirms delivery of ticket " + ticket);
                _messageQueueService.MarkQueueItemAsDelivered(ticket);
                transaction.Commit();
                return true;
            }
        }

        public bool RegisterAsZombie(string userName, string password, string zombieName)
        {
            var user = _controllUserRepository.GetByUserName(userName);

            if (user == null || user.Password != password)
                return false;

            if (user.Zombies.SingleOrDefault(z => z.Name == zombieName) != null)
                return false;

            var zombie = new Zombie
                {
                    ConnectionId = Context.ConnectionId,
                    Name = zombieName,
                    Activities = new List<Activity>()
                };

            using (var transaction = Session.BeginTransaction())
            {
                user.Zombies.Add(zombie);
                _controllUserRepository.Update(user);

                transaction.Commit();
            }

            return true;
        }

        public Task OnDisconnect()
        {
            var state = GetZombieState();

            var user = _controllUserRepository.GetByUserName(state.UserName);
            var zombie = user.Zombies.SingleOrDefault(z => z.ConnectionId == Context.ConnectionId);

            Console.Write("Zombie ");

            if (zombie != null)
            {
                using (var transaction = Session.BeginTransaction())
                {
                    zombie.ConnectionId = null;
                    _controllUserRepository.Update(user);

                    transaction.Commit();
                }
            }

            Console.WriteLine("disconnected.");

            return null;
        }

        [ExcludeFromCodeCoverage]
        public Task OnConnect()
        {
            Console.WriteLine("Zombie connected");
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

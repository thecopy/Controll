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

        private void EnsureZombieAuthentication()
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
                throw new AuthenticationException();
            }
        }

        public bool LogOn(string password)
        {
            var state = GetZombieState();
            var user = _controllUserRepository.GetByUserName(state.UserName);

            if (user == null)
                throw new AuthenticationException();

            if (user.Password != password)
                throw new AuthenticationException();

            var zombie = user.GetZombieByName(state.Name);
            if (zombie == null)
                throw new ArgumentException("Zombie " + state.Name + " does not exist");

            zombie.ConnectionId = Context.ConnectionId;

            using (var transaction = Session.BeginTransaction())
            {
                _controllUserRepository.Update(user);
                transaction.Commit();
            }

            return true;
        }

        public bool QueueItemDelivered(Guid ticket)
        {
            EnsureZombieAuthentication();
            using (var transaction = Session.BeginTransaction())
            {
                _messageQueueService.MarkQueueItemAsDelivered(ticket);
                transaction.Commit();
                return true;
            }
        }

        public bool RegisterAsZombie(string password)
        {
            var state = GetZombieState();

            var user = _controllUserRepository.GetByUserName(state.UserName);

            if (user == null || user.Password != password)
                throw new AuthenticationException();

            if (user.Zombies.SingleOrDefault(z => z.Name == state.Name) != null)
                throw new ArgumentException();

            var zombie = new Zombie
                {
                    ConnectionId = Context.ConnectionId,
                    Name = state.Name,
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

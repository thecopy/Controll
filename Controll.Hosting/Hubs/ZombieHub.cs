using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Repositories;
using Controll.Hosting.Models;
using Controll.Hosting.Services;
using SignalR;
using SignalR.Hubs;

namespace Controll.Hosting.Hubs
{
    public class ZombieHub : BaseHub, IDisconnect, IConnected
    {
        private readonly IControllUserRepository userRepository;
        private readonly IActivityService activityService;
        private readonly IGenericRepository<Activity> activityRepository;
        private readonly IMessageQueueService messageQueueService;

        public ZombieHub(
            IControllUserRepository userRepository, 
            IActivityService activityService,
            IGenericRepository<Activity> activityRepository, 
            IMessageQueueService messageQueueService)
            : base(activityRepository)
        {
            this.userRepository = userRepository;
            this.activityService = activityService;
            this.activityRepository = activityRepository;
            this.messageQueueService = messageQueueService;
        }

        private ZombieState GetZombieState()
        {
            // TODO: Gör UserName:string -> User:ControllUser
            var state = new ZombieState
                {
                    Name = (string) Caller.ZombieName, 
                    UserName = (string) Caller.BelongsToUser
                };

            return state;
        }

        private void EnsureZombieAuthentication()
        {
            var claimedBelongingToUserName = (string)Caller.BelongsToUser;
            var claimedZombieName = (string) Caller.ZombieName;

            var user = userRepository.GetByUserName(claimedBelongingToUserName);

            if (user == null 
                || user.UserName.ToLower() != claimedBelongingToUserName.ToLower()
                || user.Zombies.SingleOrDefault(z => z.Name == claimedZombieName && z.ConnectionId == Context.ConnectionId) == null)
            {
                throw new AuthenticationException();
            }
        }

        public bool LogOn(string password)
        {
            var state = GetZombieState();
            var user = userRepository.GetByUserName(state.UserName);

            if (user == null)
                throw new AuthenticationException();

            if (user.Password != password)
                throw new AuthenticationException();

            var zombie = user.GetZombieByName(state.Name);
            if(zombie == null)
                throw new ArgumentException("Zombie " + state.Name + " does not exist");

            zombie.ConnectionId = Context.ConnectionId;
            
            userRepository.Update(user);

            return true;
        }

        public bool QueueItemDelivered(Guid ticket)
        {
            EnsureZombieAuthentication();
            messageQueueService.MarkQueueItemAsDelivered(ticket);
            return true;
        }
        
        public bool RegisterAsZombie(string password)
        {
            var state = GetZombieState();

            var user = userRepository.GetByUserName(state.UserName);

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

            user.Zombies.Add(zombie);
            userRepository.Update(user);

            return true;
        }

        public Task Disconnect()
        {
            var state = GetZombieState();
            var user = userRepository.GetByUserName(state.UserName);
            var zombie = user.Zombies.SingleOrDefault(z => z.ConnectionId == Context.ConnectionId);
            
            Console.Write("Zombie ");

            if(zombie != null)
            {
                zombie.ConnectionId = null;
                
                userRepository.Update(user);

                Console.Write(zombie.Name + " ");
            }

            Console.WriteLine("disconnected.");

            return null;
        }

        [ExcludeFromCodeCoverage]
        public Task Connect()
        {
            Console.WriteLine("Zombie connected");
            return null;
        }

        [ExcludeFromCodeCoverage]
        public Task Reconnect(IEnumerable<string> groups)
        {
            Console.WriteLine("Reconnected");
            return null;
        }
    }
}

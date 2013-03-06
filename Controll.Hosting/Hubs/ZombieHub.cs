using System;
using System.Collections.Generic;
using System.Linq;
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

        private ZombieState GetState()
        {
            // TODO: Gör UserName:string -> User:ControllUser
            var state = new ZombieState
                {
                    Name = (string) Caller.ZombieName, 
                    UserName = (string) Caller.BelongsToUser
                };

            return state;
        }

        public bool LogOn(string password)
        {
            var state = GetState();
            var user = userRepository.GetByUserName(state.UserName);

            if (user == null)
                return false;

            if (user.Password != password)
                return false;

            var zombie = user.GetZombieByName(state.Name);
            if(zombie == null)
                throw new InvalidOperationException("Zombie " + state.Name + " does not exist");

            zombie.ConnectionId = Context.ConnectionId;
            
            userRepository.Update(user);

            return true;
        }

        public bool QueueItemDelivered(Guid ticket)
        {
            messageQueueService.MarkQueueItemAsDelivered(ticket);
            return true;
        }

        public void ActivityMessage(Guid ticket, ActivityMessageType type, string message)
        {
            Console.WriteLine("ZombieHub: Message " + type + ": " + message);
            activityService.InsertActivityLogMessage(ticket, type, message);
        }
        
        public bool RegisterAsZombie()
        {
            var state = GetState();

            var user = userRepository.GetByUserName(state.UserName);

            if (user == null || user.Zombies.Any(z => z.Name == state.Name)) // Unikt namn
                return false;

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

        public void NotifyActivityDownloadComplete(Guid ticket)
        {
            var owner = messageQueueService.GetQueueItem(ticket).Owner;
            
            Console.WriteLine(owner.UserName + "'s zombie have downloaded an activity and wishes to notify the user");

            // Notify all connecting client for that user
            GlobalHost.ConnectionManager.GetHubContext<ClientHub>().Clients[owner.UserName].ActivityDownloadCompleted(ticket);
        }

        
        public byte[] DownloadActivityBinary(Guid key)
        {
            throw new NotImplementedException();
            //// TODO: Implement this method
            //var data = activityService.GetActivityBinaryData(key);
            //if(data != null && data.Length > 0)
            //{
            //    // TODO: Refactor! This shouldn't be happening here
            //    var state = GetState();
            //    activityService.AddActivityToZombie(state.Name, state.UserName, key);
            //}

            //return data;
        }

        public Task Disconnect()
        {
            var user = userRepository.GetByConnectionId(Context.ConnectionId);
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

        public Task Connect()
        {
            Console.WriteLine("Zombie connected");
            return null;
        }

        public Task Reconnect(IEnumerable<string> groups)
        {
            Console.WriteLine("Reconnected");
            return null;
        }
    }
}

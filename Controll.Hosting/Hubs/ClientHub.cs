using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using SignalR.Hubs;

namespace Controll.Hosting.Hubs
{
    public class ClientHub : BaseHub, IDisconnect, IConnected
    {
        private readonly IControllUserRepository controllUserRepository;
        private readonly IMessageQueueService messageQueueService;
        private readonly IGenericRepository<Activity> activityRepository;
        private readonly IActivityService activityService;

        public ClientHub(
            IControllUserRepository controllUserRepository, 
            IMessageQueueService messageQueueService, 
            IGenericRepository<Activity> activityRepository, 
            IActivityService activityService) : base(activityRepository)
        {
            this.controllUserRepository = controllUserRepository;
            this.messageQueueService = messageQueueService;
            this.activityRepository = activityRepository;
            this.activityService = activityService;
        }

        private ControllUser GetUser()
        {
            string userName = (string)Caller.UserName;

            var user = controllUserRepository.GetByUserName(userName);

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

            controllUserRepository.Update(user);

            return true;
        }

        // TODO: Skriv en ViewModel för denna röra så man kan läsa in på PC-klienten också
        public IEnumerable<object> GetAllZombies()
        {
            var user = GetUser();
            Console.WriteLine(user.UserName + " is fetching all zombies");

            foreach (Zombie z in user.Zombies)
            {
                yield return new 
                    {
                        Activities = z.Activities.Select(a => new
                        {
                            a.Name,
                            a.Id,
                            a.CreatorName,
                            Information = a.Description,
                            UpdatedWhen = a.LastUpdated,
                            Commands = a.Commands.Select(c => new
                            {
                               c.Label,
                               c.Name,
                               Parameters = c.ParameterDescriptors.Select(p => new
                                   {
                                       p.Label,
                                       p.Description,
                                       p.Name
                                   })
                            })
                        }),
                        IsOnline = !string.IsNullOrEmpty(z.ConnectionId),
                        z.Name
                    };
            }
        }
        
        public IEnumerable<ActivityViewModel> GetActivitesInstalledOnZombie(string zombieName)
        {
            EnsureUserIsLoggedIn();
            var user = GetUser();

            return user.GetZombieByName(zombieName).Activities.Select(ViewModelHelper.CreateViewModel);
        }

        public bool RegisterUser(string userName, string password, string email)
        {
            if (controllUserRepository.GetByUserName(userName) != null
                || controllUserRepository.GetByEMail(email) != null)
                return false;

            var newUser = new ControllUser()
                {
                    EMail = email,
                    UserName = userName,
                    Password = password
                };

            controllUserRepository.Add(newUser);
            return true;
        }

        public Guid DownloadActivityAtZombie(string zombieName, Guid activityKey)
        {
            var user = GetUser();

            var activity = activityRepository.Get(activityKey);

            return activity == null ? 
                Guid.Empty : 
                messageQueueService.InsertActivityDownloadOrder(user.GetZombieByName(zombieName), activity);
        }

        public bool IsZombieOnline(string zombieName)
        {
            EnsureUserIsLoggedIn();

            var user = GetUser();
            var zombie = user.GetZombieByName(zombieName);

            Console.WriteLine("Checking online status for zombie " + zombieName + " for user " + user.UserName);

            return zombie != null && zombie.IsOnline();
        }

        public IEnumerable<object> GetActivityLogMessages(Guid ticket)
        {
            EnsureUserIsLoggedIn();

            return ((ActivityInvocationQueueItem) messageQueueService.GetQueueItem(ticket)).MessageLog;
        }
        
        public Guid GetLatestStartedActivityTicket(string zombieName, Guid activityKey)
        {
            EnsureUserIsLoggedIn();

            var user = GetUser();
            var zombie = user.GetZombieByName(zombieName);

            var ticket = activityService.GetLatestStartedActivity(user, zombie, activityKey);
            
            return ticket;
        }

        public Guid StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters, string commandName)
        {
            EnsureUserIsLoggedIn();
            var user = GetUser();

            Console.WriteLine("User '{0}' is requesting to start activity", user.UserName);

            var zombie = user.GetZombieByName(zombieName);
            if (zombie == null)
                throw new ArgumentException("Invalid Zombie Name");

            var activity = zombie.GetActivity(activityKey);
            if (activity == null)
                throw new ArgumentException("Invalid Activity Key");

            var ticket = messageQueueService.InsertActivityInvocation(zombie, activity, parameters, commandName);

            Console.WriteLine("Queueing activity " + activity.Name + " on zombie " + zombie.Name);

            return ticket;
        }

        private void EnsureUserIsLoggedIn()
        {
            var claimedUserName = (string) Caller.UserName;

            var user = controllUserRepository.GetByConnectionId(Context.ConnectionId);

            if(user == null || user.UserName.ToLower() != claimedUserName.ToLower())
                throw new AuthenticationException();
        }

        public Task Disconnect()
        {
            var user = controllUserRepository.GetByConnectionId(Context.ConnectionId);
            var client = user.ConnectedClients.SingleOrDefault(z => z.ConnectionId == Context.ConnectionId);

            Console.Write("One of " + user.UserName + "'s clients disconnected");

            if (client != null)
            {
                user.ConnectedClients.Remove(client);
                controllUserRepository.Update(user);
            }

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

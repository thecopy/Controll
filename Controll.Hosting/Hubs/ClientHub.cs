using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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
        private readonly IControllUserRepository _controllUserRepository;
        private readonly IMessageQueueService _messageQueueService;
        private readonly IGenericRepository<Activity> _activityRepository;
        private readonly IActivityService _activityService;

        public ClientHub(
            IControllUserRepository controllUserRepository, 
            IMessageQueueService messageQueueService, 
            IGenericRepository<Activity> activityRepository, 
            IActivityService activityService) : base(activityRepository)
        {
            _controllUserRepository = controllUserRepository;
            _messageQueueService = messageQueueService;
            _activityRepository = activityRepository;
            _activityService = activityService;
        }

        private ControllUser GetUser()
        {
            string userName = (string)Caller.UserName;

            var user = _controllUserRepository.GetByUserName(userName);

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

            _controllUserRepository.Update(user);

            return true;
        }

        [RequiresAuthorization]
        public IEnumerable<ZombieViewModel> GetAllZombies()
        {
            EnsureUserIsLoggedIn();

            var user = GetUser();
            Console.WriteLine(user.UserName + " is fetching all zombies");

            foreach (Zombie z in user.Zombies)
            {
                yield return ViewModelHelper.CreateViewModel(z);
            }
        }

        [RequiresAuthorization]
        public IEnumerable<ActivityViewModel> GetActivitesInstalledOnZombie(string zombieName)
        {
            EnsureUserIsLoggedIn();
            var user = GetUser();

            return user.GetZombieByName(zombieName).Activities.Select(ViewModelHelper.CreateViewModel);
        }

        public bool RegisterUser(string userName, string password, string email)
        {
            if (_controllUserRepository.GetByUserName(userName) != null
                || _controllUserRepository.GetByEMail(email) != null)
                return false;

            var newUser = new ControllUser()
                {
                    EMail = email,
                    UserName = userName,
                    Password = password
                };

            _controllUserRepository.Add(newUser);
            return true;
        }

        [RequiresAuthorization]
        public bool IsZombieOnline(string zombieName)
        {
            EnsureUserIsLoggedIn();

            var user = GetUser();
            var zombie = user.GetZombieByName(zombieName);

            if(zombie == null)
                throw new ArgumentException("Zombie does not exist", "zombieName");

            Console.WriteLine("Checking online status for zombie " + zombieName + " for user " + user.UserName);

            return zombie.IsOnline();
        }

        [RequiresAuthorization]
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

            var ticket = _messageQueueService.InsertActivityInvocation(zombie, activity, parameters, commandName);

            Console.WriteLine("Queueing activity " + activity.Name + " on zombie " + zombie.Name);

            return ticket;
        }

        private void EnsureUserIsLoggedIn()
        {
            var claimedUserName = (string) Caller.UserName;

            var user = _controllUserRepository.GetByConnectionId(Context.ConnectionId);

            if(user == null || user.UserName.ToLower() != claimedUserName.ToLower())
                throw new AuthenticationException();
        }

        public Task Disconnect()
        {
            var user = _controllUserRepository.GetByConnectionId(Context.ConnectionId);
            var client = user.ConnectedClients.SingleOrDefault(z => z.ConnectionId == Context.ConnectionId);

            Console.Write("One of " + user.UserName + "'s clients disconnected");

            if (client != null)
            {
                user.ConnectedClients.Remove(client);
                _controllUserRepository.Update(user);
            }

            return null;
        }


        [ExcludeFromCodeCoverage]
        public Task Connect()
        {
            Console.WriteLine("Client connected");
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

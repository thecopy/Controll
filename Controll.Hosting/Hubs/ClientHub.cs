using System;
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
        private IGenericRepository<Activity> _activityRepository;
        private IActivityService _activityService;

        public ClientHub(IControllUserRepository controllUserRepository,
                         IMessageQueueService messageQueueService,
                         IGenericRepository<Activity> activityRepository,
                         IActivityService activityService,
                         ISession session) : base(session)
        {
            _controllUserRepository = controllUserRepository;
            _messageQueueService = messageQueueService;
            _activityRepository = activityRepository;
            _activityService = activityService;
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

            ControllUser user = GetUser();

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
            EnsureUserIsLoggedIn();

            ControllUser user = GetUser();
            Console.WriteLine(user.UserName + " is fetching all zombies");

            foreach (var z in user.Zombies)
            {
                yield return ViewModelHelper.CreateViewModel(z);
            }
        }

        [RequiresAuthorization]
        public IEnumerable<ActivityViewModel> GetActivitesInstalledOnZombie(string zombieName)
        {
            EnsureUserIsLoggedIn();
            ControllUser user = GetUser();

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

            using (ITransaction transaction = Session.BeginTransaction())
            {
                _controllUserRepository.Add(newUser);
                transaction.Commit();
            }
            return true;
        }


        [RequiresAuthorization]
        public bool IsZombieOnline(string zombieName)
        {
            EnsureUserIsLoggedIn();

            ControllUser user = GetUser();
            Zombie zombie = user.GetZombieByName(zombieName);

            if (zombie == null)
                throw new ArgumentException("Zombie does not exist", "zombieName");

            Console.WriteLine("Checking online status for zombie " + zombieName + " for user " + user.UserName);

            return zombie.IsOnline();
        }

        [RequiresAuthorization]
        public Guid StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters,
                                  string commandName)
        {
            EnsureUserIsLoggedIn();
            ControllUser user = GetUser();

            Console.WriteLine("User '{0}' is requesting to start activity", user.UserName);

            Zombie zombie = user.GetZombieByName(zombieName);
            if (zombie == null)
                throw new ArgumentException("Invalid Zombie Name");

            Activity activity = zombie.GetActivity(activityKey);
            if (activity == null)
                throw new ArgumentException("Invalid Activity Key");

            using (ITransaction transaction = Session.BeginTransaction())
            {
                Guid ticket = _messageQueueService.InsertActivityInvocation(zombie, activity, parameters, commandName);
                transaction.Commit();

                Console.WriteLine("Queueing activity " + activity.Name + " on zombie " + zombie.Name);
                return ticket;
            }
        }

        private void EnsureUserIsLoggedIn()
        {
            var claimedUserName = (string) Clients.Caller.UserName;

            var user = _controllUserRepository.GetByConnectionId(Context.ConnectionId);

            if (user == null || user.UserName.ToLower() != claimedUserName.ToLower())
                throw new AuthenticationException();
        }

        public Task OnDisconnect()
        {
            var user = _controllUserRepository.GetByConnectionId(Context.ConnectionId);
            var client = user.ConnectedClients.SingleOrDefault(z => z.ConnectionId == Context.ConnectionId);

            Console.Write("One of " + user.UserName + "'s clients disconnected");

            if (client != null)
            {
                using (ITransaction transaction = Session.BeginTransaction())
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

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
using ParameterDescriptor = Controll.Hosting.Models.ParameterDescriptor;

namespace Controll.Hosting.Hubs
{
    public class ZombieHub : BaseHub
    {
        private readonly IControllUserRepository _controllUserRepository;
        private readonly IMessageQueueService _messageQueueService;
        private readonly IActivityMessageLogService _activityService;

        public ZombieHub(IControllUserRepository controllUserRepository,
                         IMessageQueueService messageQueueService,
                         IActivityMessageLogService activityService,
                         ISession session) : base(session)
        {
            _controllUserRepository = controllUserRepository;
            _messageQueueService = messageQueueService;
            _activityService = activityService;
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
                || user.Zombies.SingleOrDefault(z => z.Name == claimedZombieName && z.ConnectionId == Context.ConnectionId) ==
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

        public void SynchronizeActivities(ICollection<ActivityViewModel> activities)
        {
            if (false == EnsureZombieAuthentication())
                return;

            var state = GetZombieState();
            var user = _controllUserRepository.GetByUserName(state.UserName);
            var zombie = user.GetZombieByName(state.Name);

            Console.WriteLine("Synchronizing activities for zombie " + zombie.Name + " for user " + user.UserName);
            
            using (var transaction = Session.BeginTransaction())
            {
                for (int i = 0; i < zombie.Activities.Count(syncedActivity => activities.Count(a => a.Key == syncedActivity.Id) == 1); i++)
                {
                    var syncedActivity = zombie.Activities.ToList().Where(s => activities.Count(a => a.Key == s.Id) == 1).ToList()[i];
                    Console.WriteLine(syncedActivity.Name + ": existing in database. Updating (ONLY VERSION!!!!)...");
                    var installedActivity = activities.Single(a => a.Key == syncedActivity.Id);

                    zombie.Activities[i].Version = installedActivity.Version;
                }

                foreach (var syncedActivity in zombie.Activities.ToList().Where(syncedActivity => activities.Count(a => a.Key == syncedActivity.Id) == 0))
                {
                    Console.WriteLine(syncedActivity.Name + ": Not installed at zombie. Removing...");
                    zombie.Activities.Remove(syncedActivity);
                }

                foreach (var installedActivity in activities.Where(installedActivity => zombie.Activities.Count(a => a.Id == installedActivity.Key) == 0))
                {
                    Console.WriteLine(installedActivity.Name + ": Adding activity...");
                    zombie.Activities.Add(installedActivity.CreateConcreteClass());
                }

                _controllUserRepository.Update(user);
                transaction.Commit();
            }
        }

        public void ActivityMessage(Guid ticket, ActivityMessageType type, string message)
        {
            using (var transaction = Session.BeginTransaction())
            {
                _activityService.InsertActivityLogMessage(ticket, type, message);
                transaction.Commit();
            }
            
            _messageQueueService.InsertActivityMessage(ticket, type, message);
        }

        public void ActivityResult(Guid ticket, object result)
        {
            Console.WriteLine("Activity result recieved.");
            _messageQueueService.InsertActivityResult(ticket, result);
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

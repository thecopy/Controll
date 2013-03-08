using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Criterion;
using Remotion.Linq.Collections;
using SignalR;

namespace Controll.Hosting.Services
{
    public class ActivityService : IActivityService
    {
        private readonly IGenericRepository<ActivityInvocationQueueItem> invocationQueuItemRepository;
        private readonly IGenericRepository<Activity> activityRepository;
        private readonly IGenericRepository<Zombie> zombieRepository;

        public ActivityService(
            IGenericRepository<ActivityInvocationQueueItem> invocationQueuItemRepository,
            IGenericRepository<Activity> activityRepository,
            IGenericRepository<Zombie> controllUserRepository)
        {
            this.invocationQueuItemRepository = invocationQueuItemRepository;
            this.activityRepository = activityRepository;
            this.zombieRepository = controllUserRepository;
        }

        public event EventHandler<Tuple<Guid, ActivityInvocationLogMessage>> NewActivityLogItem;

        public void OnNewActivityLogItem(Guid ticket, ActivityInvocationLogMessage item)
        {
            var tuple = new Tuple<Guid, ActivityInvocationLogMessage>(ticket, item);

            EventHandler<Tuple<Guid, ActivityInvocationLogMessage>> handler = NewActivityLogItem;
            if (handler != null) handler(this, tuple);
        }
        
        public void UpdateLogWithResponse(Guid ticket, string response)
        {
            var queueItem = invocationQueuItemRepository.Get(ticket);

            queueItem.Responded = DateTime.Now;
            queueItem.Response = response;

            invocationQueuItemRepository.Update(queueItem);
        }

        public void InsertActivityLogMessage(Guid ticket, ActivityMessageType type, string message)
        {
            var invocationQueueItem = invocationQueuItemRepository.Get(ticket);

            var messageLogItem = new ActivityInvocationLogMessage
                {
                    Date = DateTime.Now,
                    Type = type,
                    Message = message
                };

            invocationQueueItem.MessageLog.Add(messageLogItem);

            invocationQueuItemRepository.Update(invocationQueueItem);

            OnNewActivityLogItem(ticket, messageLogItem);
        }

        public Guid GetLatestStartedActivity(ControllUser user, Zombie zombie, Guid guid)
        {
            // SÅ HÄR SKALL EJ!!! GÖRAS I RELEASE!!!!
#warning Temporär lösning
            var activity = invocationQueuItemRepository.GetAll()
                .OrderByDescending(s => s.RecievedAtCloud)
                .FirstOrDefault(a => 
                    a.Activity.Id == guid &&
                    a.Reciever.Id == zombie.Id);

            return activity == null ? Guid.Empty : activity.Ticket;
        }

        public byte[] GetActivityBinaryData(Guid activityKey)
        {
            var activity = activityRepository.Get(activityKey);
            return File.ReadAllBytes(activity.FilePath);
        }

        public void AddActivityToZombie(string zombieName, ControllUser user, Guid key)
        {
            var zombie = user.GetZombieByName(zombieName);

            if (zombie == null)
                return;

            zombie.Activities.Add(activityRepository.Get(key));

            zombieRepository.Update(zombie);
        }
    }

}

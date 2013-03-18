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

namespace Controll.Hosting.Services
{
    public class ActivityService : IActivityService
    {
        private readonly IGenericRepository<ActivityInvocationQueueItem> _invocationQueueItemRepository;
        private readonly IGenericRepository<Activity> _activityRepository;
        private readonly IGenericRepository<Zombie> _zombieRepository;

        public ActivityService(
            IGenericRepository<ActivityInvocationQueueItem> invocationQueuItemRepository,
            IGenericRepository<Activity> activityRepository,
            IGenericRepository<Zombie> controllUserRepository)
        {
            this._invocationQueueItemRepository = invocationQueuItemRepository;
            this._activityRepository = activityRepository;
            this._zombieRepository = controllUserRepository;
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
            var queueItem = _invocationQueueItemRepository.Get(ticket);

            queueItem.Responded = DateTime.Now;
            queueItem.Response = response;

            _invocationQueueItemRepository.Update(queueItem);
        }

        public void InsertActivityLogMessage(Guid ticket, ActivityMessageType type, string message)
        {
            var invocationQueueItem = _invocationQueueItemRepository.Get(ticket);

            var messageLogItem = new ActivityInvocationLogMessage
                {
                    Date = DateTime.Now,
                    Type = type,
                    Message = message
                };

            invocationQueueItem.MessageLog.Add(messageLogItem);

            _invocationQueueItemRepository.Update(invocationQueueItem);

            OnNewActivityLogItem(ticket, messageLogItem);
        }

        public Guid GetLatestStartedActivity(ControllUser user, Zombie zombie, Guid activityId)
        {
            // SÅ HÄR SKALL EJ!!! GÖRAS I RELEASE!!!!
#warning Temporär lösning
            var activity = _invocationQueueItemRepository.GetAll()
                .OrderByDescending(s => s.RecievedAtCloud)
                .FirstOrDefault(a => 
                    a.Activity.Id == activityId &&
                    a.Reciever.Id == zombie.Id);

            return activity == null ? Guid.Empty : activity.Ticket;
        }

        public void AddActivityToZombie(string zombieName, ControllUser user, Guid key)
        {
            var zombie = user.GetZombieByName(zombieName);

            if (zombie == null)
                return;

            zombie.Activities.Add(_activityRepository.Get(key));

            _zombieRepository.Update(zombie);
        }
    }

}

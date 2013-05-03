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
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Criterion;
using Remotion.Linq.Collections;

namespace Controll.Hosting.Services
{
    public class ActivityMessageLogService : IActivityMessageLogService
    {
        private readonly IGenericRepository<QueueItem> _invocationQueueItemRepository;

        public ActivityMessageLogService(
            IGenericRepository<QueueItem> invocationQueuItemRepository)
        {
            this._invocationQueueItemRepository = invocationQueuItemRepository;
        }

        public void UpdateLogWithResponse(Guid ticket, string response)
        {
            var queueItem = (ActivityInvocationQueueItem)_invocationQueueItemRepository.Get(ticket);

            queueItem.Responded = DateTime.Now;
            queueItem.Response = response;

            _invocationQueueItemRepository.Update(queueItem);
        }

        public void InsertActivityLogMessage(Guid ticket, ActivityMessageType type, string message)
        {
            var invocationQueueItem = (ActivityInvocationQueueItem)_invocationQueueItemRepository.Get(ticket);

            var messageLogItem = new ActivityInvocationLogMessage
                {
                    Date = DateTime.Now,
                    Type = type,
                    Message = message
                };

            invocationQueueItem.MessageLog.Add(messageLogItem);

            _invocationQueueItemRepository.Update(invocationQueueItem);
        }

        public ICollection<ActivityInvocationLogMessage> GetActivityMessagesForInvocationTicket(Guid ticket)
        {
            var invocationQueueItem = (ActivityInvocationQueueItem)_invocationQueueItemRepository.Get(ticket);

            return invocationQueueItem.MessageLog;
        }

        public ActivityInvocationQueueItem GetActivityInvocationFromTicket(Guid ticket)
        {
            var invocationQueueItem = (ActivityInvocationQueueItem)_invocationQueueItemRepository.Get(ticket);

            return invocationQueueItem;
        }
    }

}

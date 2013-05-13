using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Criterion;
using Remotion.Linq.Collections;
using ActivityInvocationLogMessage = Controll.Hosting.Models.ActivityInvocationLogMessage;

namespace Controll.Hosting.Services
{
    public class ActivityMessageLogService : IActivityMessageLogService
    {
        private readonly IGenericRepository<ActivityInvocationQueueItem> _invocationQueueItemRepository;
        private readonly IGenericRepository<ActivityResultQueueItem> _resultQueueItemRepostiory;

        public ActivityMessageLogService(
            IGenericRepository<ActivityInvocationQueueItem> invocationQueuItemRepository,
            IGenericRepository<ActivityResultQueueItem> resultQueueItemRepostiory)
        {
            this._invocationQueueItemRepository = invocationQueuItemRepository;
            _resultQueueItemRepostiory = resultQueueItemRepostiory;
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

        public IList<ActivityInvocationLogBookViewModel> GetActivityLog(Zombie zombie)
        {
            var queueItems = _invocationQueueItemRepository.Query
                                                           .Where(x => x.Reciever.Id == zombie.Id)
                                                           .ToList();

            var returnList = new List<ActivityInvocationLogBookViewModel>();
            foreach (var queueItem in queueItems)
            {
                var item = new ActivityInvocationLogBookViewModel();
                var command = queueItem.Activity.Commands.SingleOrDefault(c => c.Name == queueItem.CommandName);

                item.ActivityName = queueItem.Activity.Name;
                item.CommandLabel = command != null ? command.Label : queueItem.CommandName + " (intermidiate)";
                item.Parameters = queueItem.Parameters;
                item.Delivered = queueItem.Delivered;

                item.Messages = queueItem.MessageLog.Select(msg =>
                                                            new ActivityInvocationLogMessageViewModel
                                                                {
                                                                    Date = msg.Date,
                                                                    Message = msg.Message,
                                                                    MessageType = msg.Type
                                                                }).ToList();
                returnList.Add(item);
            }

            return returnList;

        }

        public IList<ActivityIntermidiateCommandViewModel> GetUndeliveredIntermidiates(Zombie zombie)
        {
            var resultItems = _resultQueueItemRepostiory.Query;
            var invocationItems = _invocationQueueItemRepository.Query;

            var joinedSequence = from x in resultItems
                                 join y in invocationItems on x.InvocationTicket equals y.Ticket
                                 select new ActivityIntermidiateCommandViewModel(
                                     x.ActivityCommand.CreateViewModel(), 
                                     y.Activity.CreateViewModel(),
                                     x.Ticket);
            
            return joinedSequence.ToList();
        }
    }

}

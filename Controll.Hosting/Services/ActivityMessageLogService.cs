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
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using Remotion.Linq.Collections;
using ActivityInvocationLogMessage = Controll.Hosting.Models.ActivityInvocationLogMessage;

namespace Controll.Hosting.Services
{
    public class ActivityMessageLogService : IActivityMessageLogService
    {
        private readonly ISession _session;

        public ActivityMessageLogService(ISession session)
        {
            _session = session;
        }

        public void UpdateLogWithResponse(Guid ticket, string response)
        {
            var queueItem = _session.Get<ActivityInvocationQueueItem>(ticket);

            queueItem.Responded = DateTime.Now;
            queueItem.Response = response;

            _session.Update(queueItem);
        }

        public void InsertActivityLogMessage(Guid ticket, ActivityMessageType type, string message)
        {
            var invocationQueueItem = _session.Get<ActivityInvocationQueueItem>(ticket);

            var messageLogItem = new ActivityInvocationLogMessage
                {
                    Date = DateTime.Now,
                    Type = type,
                    Message = message
                };

            invocationQueueItem.MessageLog.Add(messageLogItem);

            _session.Update(invocationQueueItem);
        }

        public ICollection<ActivityInvocationLogMessage> GetActivityMessagesForInvocationTicket(Guid ticket)
        {
            var invocationQueueItem = _session.Get<ActivityInvocationQueueItem>(ticket);

            return invocationQueueItem.MessageLog;
        }

        public ActivityInvocationQueueItem GetActivityInvocationFromTicket(Guid ticket)
        {
            var invocationQueueItem = _session.Get<ActivityInvocationQueueItem>(ticket);

            return invocationQueueItem;
        }

        public IList<ActivityInvocationLogBookViewModel> GetActivityLog(Zombie zombie)
        {
            var queueItems = _session.CreateCriteria<ActivityInvocationQueueItem>()
                                     .CreateCriteria("Reciever")
                                     .Add(Restrictions.Eq("Id", zombie.Id))
                                     .List<ActivityInvocationQueueItem>();

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
            var queueItems = _session.CreateCriteria<ActivityResultQueueItem>()
                         .CreateCriteria("Sender")
                         .Add(Restrictions.Eq("Id", zombie.Id))
                         .List<ActivityResultQueueItem>();

            return queueItems.Select(qi => new ActivityIntermidiateCommandViewModel(
                                               qi.ActivityCommand.CreateViewModel(),
                                               qi.Activity.CreateViewModel(),
                                               qi.InvocationTicket
                                               )).ToList();
        }
    }

}

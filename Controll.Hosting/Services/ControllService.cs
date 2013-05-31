using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Microsoft.AspNet.SignalR.Infrastructure;
using NHibernate;
using NHibernate.Criterion;

namespace Controll.Hosting.Services
{
    public class ControllService : IControllService
    {
        private readonly ISession _session;
        private readonly IControllRepository _controllRepository;
        private readonly IConnectionManager _connectionManager;

        public ControllService(ISession session, IControllRepository controllRepository, IConnectionManager connectionManager)
        {
            _session = session;
            _controllRepository = controllRepository;
            _connectionManager = connectionManager;
        }

        public QueueItem InsertActivityInvocation(Zombie zombie, Activity activity, IDictionary<string, string> parameters, string commandName, string connectionId)
        {
            var queueItem = new ActivityInvocationQueueItem
            {
                Activity = activity,
                Reciever = zombie,
                Parameters = parameters,
                CommandName = commandName,
                Sender = zombie.Owner,
                RecievedAtCloud = DateTime.Now
            };
            _session.Save(queueItem);

            return queueItem;
        }

        /// <summary>
        /// Marks the QueueItem as delivered and created a delivery acknowledgement to send to the original initiator
        /// </summary>
        /// <param name="ticket">The ticket of the QueueItem which to mark as delivered</param>
        public void MarkQueueItemAsDelivered(Guid ticket)
        {
            var queueItem = _session.Get<QueueItem>(ticket);

            queueItem.Delivered = DateTime.Now;
            _session.Update(queueItem);

            // Do not add the delivered queue item into the queue. This should only be sent to the original sender
            // of the message which has been marked as delivered. And if he is not online we dont care.
            // The message will be marked as delivered in the log here on server side anyway.

            foreach (var connectionId in queueItem.Sender.ConnectedClients.Select(x => x.ConnectionId))
                SendDeliveryAcknowledgement(ticket, connectionId);
        }

        public QueueItem InsertPingMessage(Zombie zombie, string senderConnectionId)
        {
            var queueItem = new PingQueueItem
            {
                Reciever = zombie,
                Sender = zombie.Owner,
                RecievedAtCloud = DateTime.Now
            };

            _session.Save(queueItem);

            return queueItem;
        }

        public void ProcessUndeliveredMessagesForZombie(Zombie zombie)
        {
            var queueItems = _controllRepository.GetUndeliveredQueueItemsForZombie(zombie.Id);

            foreach (var queueItem in queueItems)
            {
                ProcessQueueItem(queueItem);
            }
        }

        public void InsertActivityMessage(Guid ticket, ActivityMessageType type, string message)
        {
            var queueItem = _session.Get<QueueItem>(ticket);
            // We want to send this to the sender aka the invocator
            var connectedClients = queueItem.Sender.ConnectedClients;
            Console.WriteLine("Will send message to someone which have "
                + connectedClients.Count() + " clients");

            foreach (var connectionId in connectedClients.Select(x => x.ConnectionId))
            {
                Console.Write("Sending " + type + " to " + connectionId + ": ");
                SendActivityMessage(connectionId, ticket, type, message);
                Console.WriteLine(" Done");
            }
        }

        public void InsertActivityResult(Guid ticket, ActivityCommand intermidiateCommand)
        {
            var queueItem = _session.Get<ActivityInvocationQueueItem>(ticket);

            // Notice: Switch sender and reciever
            var activityResultQueueItem = new ActivityResultQueueItem
            {
                ActivityCommand = intermidiateCommand,
                RecievedAtCloud = DateTime.Now,
                Reciever = queueItem.Sender,
                Sender = queueItem.Reciever,
                InvocationTicket = ticket
            };

            _session.Save(activityResultQueueItem);

            ProcessQueueItem(activityResultQueueItem);
        }

        public void ProcessQueueItem<T>(T queueItem)
            where T : QueueItem
        {
            if (!queueItem.Reciever.ConnectedClients.Any())
                return;

            var actions = new Dictionary<QueueItemType, Action<QueueItem, string>>
                {
                    {QueueItemType.ActivityInvocation, (qi, s) => SendActivityInvocation((ActivityInvocationQueueItem) qi, s)},
                    {QueueItemType.Ping, (qi, s) => SendPing((PingQueueItem) qi, s)},
                    {QueueItemType.ActivityResult, (qi, s) => SendActivityResult((ActivityResultQueueItem) qi, s)},
                };

            if (!actions.ContainsKey(queueItem.Type))
            {
                throw new InvalidOperationException("Unkown queue item type: " + queueItem.Type);
            }

            foreach (var connectionId in queueItem.Reciever.ConnectedClients.Select(x => x.ConnectionId))
            {
                actions[queueItem.Type](queueItem, connectionId);
                Console.WriteLine("Sending " + queueItem.Type + " to " + connectionId);
            }
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

        private void SendActivityMessage(string connectionId, Guid ticket, ActivityMessageType type, string message)
        {
            _connectionManager.GetHubContext<ClientHub>().Clients.Client(connectionId)
                              .ActivityMessage(ticket, type, message);
        }

        private void SendDeliveryAcknowledgement(Guid deliveredTicked, string connectionId)
        {
            _connectionManager.GetHubContext<ClientHub>().Clients.Client(connectionId)
                              .MessageDelivered(deliveredTicked);
        }

        private void SendActivityResult(ActivityResultQueueItem queueItem, string connectionId)
        {
            _connectionManager.GetHubContext<ClientHub>().Clients.Client(connectionId)
                              .ActivityResult(queueItem.InvocationTicket, queueItem.ActivityCommand.CreateViewModel());
        }

        private void SendPing(PingQueueItem item, string connectionId)
        {
            _connectionManager.GetHubContext<ZombieHub>().Clients.Client(connectionId)
                              .Ping(item.Ticket);
        }

        private void SendActivityInvocation(ActivityInvocationQueueItem item, string connectionId)
        {
            _connectionManager.GetHubContext<ZombieHub>().Clients.Client(connectionId)
                              .InvokeActivity(item.Activity.Id, item.Ticket, item.Parameters, item.CommandName);
        }
    }
}

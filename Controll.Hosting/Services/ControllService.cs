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
using Microsoft.AspNet.SignalR;
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

        private readonly IHubContext _zombieHubContext;
        private readonly IHubContext _clientHubContext;

        public ControllService(ISession session, IControllRepository controllRepository, IConnectionManager connectionManager)
        {
            _session = session;
            _controllRepository = controllRepository;
            _connectionManager = connectionManager;

            _zombieHubContext = _connectionManager.GetHubContext<ZombieHub>();
            _clientHubContext = _connectionManager.GetHubContext<ClientHub>();
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

            var user = (ControllUser)queueItem.Sender;
            user.LogBooks.Add(new LogBook
                {
                    Activity = activity,
                    Started = DateTime.Now,
                    CommandName = commandName,
                    InvocationTicket = queueItem.Ticket,
                    Parameters = parameters
                });

            _session.Update(user);

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

        public QueueItem InsertActivityDownload(Zombie zombie, string url)
        {
            var queueItem = new DownloadActivityQueueItem
                {
                    Reciever = zombie,
                    Sender = zombie.Owner,
                    RecievedAtCloud = DateTime.Now,
                    Url = url
                };

            _session.Save(queueItem);

            return queueItem;
        }

        public void ProcessUndeliveredMessagesForZombie(Zombie zombie)
        {
            var queueItems = _controllRepository.GetUndeliveredQueueItemsForZombie(zombie.Id, 100, 0);

            foreach (var queueItem in queueItems)
            {
                ProcessQueueItem(queueItem);
            }
        }

        public void InsertActivityMessage(Guid ticket, ActivityMessageType type, string message)
        {
            var queueItem = _session.Get<ActivityInvocationQueueItem>(ticket);
            
            var user = (ControllUser) queueItem.Sender;
            AddLogMessageToLog(user, ticket, queueItem.Activity, type, message);

            // We want to send this to the sender aka the invocator
            var connectedClients = queueItem.Sender.ConnectedClients;
            foreach (var connectionId in connectedClients.Select(x => x.ConnectionId))
            {
                Console.Write("Sending " + type + " to " + connectionId + ": ");
                SendActivityMessage(connectionId, ticket, type, message);
                Console.WriteLine(" Done");
            }
        }

        private void AddLogMessageToLog(ControllUser user, Guid ticket, Activity activity, ActivityMessageType type, String message)
        {
            var book = user.LogBooks.SingleOrDefault(x => x.InvocationTicket == ticket);
            if (book == null)
            {
                book = new LogBook
                {
                    Activity = activity,
                    InvocationTicket = ticket,
                    LogMessages = new List<LogMessage>()
                };
                user.LogBooks.Add(book);
            }

            book.LogMessages.Add(new LogMessage
            {
                Date = DateTime.Now,
                Message = message,
                Type = type
            });

            _session.Update(user);
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
                    {QueueItemType.DownloadActivity, (qi, s) => SendDownloadActivity((DownloadActivityQueueItem)qi, s)}
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

        public void InsertActivitiesSynchronizedMessage(Zombie zombie)
        {
            foreach (var connectionId in zombie.Owner.ConnectedClients.Select(x => x.ConnectionId))
            {
                _clientHubContext.Clients.Client(connectionId)
                    .ZombieSynchronized(zombie.Name, zombie.Activities.Select(x => x.CreateViewModel()));
            }
        }

        private void SendDownloadActivity(DownloadActivityQueueItem qi, string connectionId)
        {
            _zombieHubContext.Clients.Client(connectionId)
                              .DownloadActivity(qi.Ticket, qi.Url);
        }

        private void SendActivityMessage(string connectionId, Guid ticket, ActivityMessageType type, string message)
        {
            _clientHubContext.Clients.Client(connectionId)
                              .ActivityMessage(ticket, type, message);
        }

        private void SendDeliveryAcknowledgement(Guid deliveredTicked, string connectionId)
        {
            _clientHubContext.Clients.Client(connectionId)
                              .MessageDelivered(deliveredTicked);
        }

        private void SendActivityResult(ActivityResultQueueItem queueItem, string connectionId)
        {
            _clientHubContext.Clients.Client(connectionId)
                              .ActivityResult(queueItem.InvocationTicket, queueItem.ActivityCommand.CreateViewModel());
        }

        private void SendPing(PingQueueItem item, string connectionId)
        {
            _zombieHubContext.Clients.Client(connectionId)
                              .Ping(item.Ticket);
        }

        private void SendActivityInvocation(ActivityInvocationQueueItem item, string connectionId)
        {
            _zombieHubContext.Clients.Client(connectionId)
                              .InvokeActivity(item.Activity.Id, item.Ticket, item.Parameters, item.CommandName);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
    public sealed class MessageQueueService : IMessageQueueService
    {
        private readonly IQueueItemRepostiory _queueItemRepository;
        private readonly IConnectionManager _connectionManager;
        public MessageQueueService(
            IQueueItemRepostiory queueItemRepository,
            IConnectionManager connectionManager)
        {
            _queueItemRepository = queueItemRepository;
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Inserts an activity invocation into the message queue. 
        /// </summary>
        /// <param name="zombie">The ZombieClient for which to invoke the activity</param>
        /// <param name="activity">The activity to invoke</param>
        /// <param name="parameters">The parameters which to pass to the activity</param>
        /// <param name="commandName">The name of the command in the activity</param>
        /// <param name="connectionId">The connection-id of the initiating client</param>
        /// <returns>The queue item ticket</returns>
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
            _queueItemRepository.Add(queueItem);

            return queueItem;
        }

        /// <summary>
        /// Marks the QueueItem as delivered and created a delivery acknowledgement to send to the original initiator
        /// </summary>
        /// <param name="ticket">The ticket of the QueueItem which to mark as delivered</param>
        public void MarkQueueItemAsDelivered(Guid ticket)
        {
            var queueItem = _queueItemRepository.Get(ticket);
            
            queueItem.Delivered = DateTime.Now;
            _queueItemRepository.Update(queueItem);

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

            _queueItemRepository.Add(queueItem);

            return queueItem;
        }

        public void ProcessUndeliveredMessagesForZombie(Zombie zombie)
        {
            var queueItems = _queueItemRepository.GetUndeliveredQueueItemsForZombie(zombie.Id);

            foreach (var queueItem in queueItems)
            {
                ProcessQueueItem(queueItem);
            }
        }

        public void InsertActivityMessage(Guid ticket, ActivityMessageType type, string message)
        {
            var queueItem = _queueItemRepository.Get(ticket);

            // We want to send this to the sender aka the invocator
            var user = (ControllUser)queueItem.Sender;
            Console.WriteLine("Will send message to " + user.UserName + " which have " 
                + user.ConnectedClients.Count + " clients");

            foreach (var connectionId in queueItem.Sender.ConnectedClients.Select(x => x.ConnectionId))
            {
                Console.Write("Sending " + type + " to " + connectionId + ": ");
                SendActivityMessage(connectionId, ticket, type, message);
                Console.WriteLine(" Done");
            }
        }

        public void InsertActivityResult(Guid ticket, ActivityCommand intermidiateCommand)
        {
            var queueItem = _queueItemRepository.Get(ticket);

            var activityResultQueueItem = new ActivityResultQueueItem
                {
                    ActivityCommand = intermidiateCommand,
                    RecievedAtCloud = DateTime.Now,
                    Reciever = queueItem.Sender,
                    Sender = queueItem.Reciever,
                    InvocationTicket = ticket
                };

            _queueItemRepository.Add(activityResultQueueItem);
            
            ProcessQueueItem(activityResultQueueItem);
        }

        public void ProcessQueueItem<T>(T queueItem) 
            where T:QueueItem
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

            foreach(var connectionId in queueItem.Reciever.ConnectedClients.Select(x => x.ConnectionId))
            {
                actions[queueItem.Type](queueItem, connectionId);
                Console.WriteLine("Sending " + queueItem.Type + " to " + connectionId);
            }
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

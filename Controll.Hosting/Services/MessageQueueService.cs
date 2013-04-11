using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Controll.Common;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
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
        public Guid InsertActivityInvocation(Zombie zombie, Activity activity, Dictionary<string, string> parameters, string commandName, string connectionId)
        {
            var queueItem = new ActivityInvocationQueueItem
                {
                    Activity = activity,
                    Reciever = zombie,
                    Parameters = parameters,
                    CommandName = commandName,
                    SenderConnectionId = connectionId,
                    RecievedAtCloud = DateTime.UtcNow
                };

            _queueItemRepository.Add(queueItem);

            ProcessQueueItem(queueItem);
            return queueItem.Ticket;
        }

        /// <summary>
        /// Marks the QueueItem as delivered and created a delivery acknowledgement to send to the original initiator
        /// </summary>
        /// <param name="ticket">The ticket of the QueueItem which to mark as delivered</param>
        public void MarkQueueItemAsDelivered(Guid ticket)
        {
            var queueItem = _queueItemRepository.Get(ticket);

            queueItem.Delivered = DateTime.UtcNow;
            _queueItemRepository.Update(queueItem);

            // Do not add the delivered queue item into the queue. This should only be sent to the original sender
            // of the message which has been marked as delivered. And if he is not online we dont care.
            // The message will be marked as delivered in the log here on server side anyway.
            SendDeliveryAcknowledgement(ticket, queueItem.SenderConnectionId);
        }

        public Guid InsertPingMessage(Zombie zombie, string senderConnectionId)
        {
            var queueItem = new PingQueueItem
            {
                Reciever = zombie,
                SenderConnectionId = senderConnectionId,
                RecievedAtCloud = DateTime.UtcNow
            };

            _queueItemRepository.Add(queueItem);

            ProcessQueueItem(queueItem);
            return queueItem.Ticket;
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

            SendActivityMessage(queueItem.SenderConnectionId, ticket, type, message);
        }

        private void ProcessQueueItem(QueueItem queueItem)
        {
            if (string.IsNullOrEmpty(queueItem.Reciever.ConnectionId))
                return;

            var type = queueItem.Type;
            switch (type)
            {
                case QueueItemType.ActivityInvocation:
                    SendActivityInvocation((ActivityInvocationQueueItem)queueItem);
                    break;
                case QueueItemType.Ping:
                    SendPing((PingQueueItem) queueItem);
                    break;
                default:
                    Console.WriteLine("FATAL ERROR IN DELIVERY QUEUE: Unkown queue item type: " + queueItem.Type);
                    break;
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

        private void SendPing(PingQueueItem item)
        {
            string connectionId = item.Reciever.ConnectionId;

            Console.WriteLine("Pinging " + item.Reciever.Name + "(" + connectionId + " ticket = " + item.Ticket + ")");
            _connectionManager.GetHubContext<ZombieHub>().Clients.All
                      .Ping(item.Ticket);
        }

        private void SendActivityInvocation(ActivityInvocationQueueItem item)
        {
            string connectionId = item.Reciever.ConnectionId;
            _connectionManager.GetHubContext<ZombieHub>().Clients.Client(connectionId)
                .InvokeActivity(item.Activity.Id, item.Ticket, item.Parameters);
        }
    }
}

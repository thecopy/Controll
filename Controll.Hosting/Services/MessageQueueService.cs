using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Microsoft.AspNet.SignalR;

namespace Controll.Hosting.Services
{
    public sealed class MessageQueueService : IMessageQueueService
    {
        private readonly IGenericRepository<QueueItem> queueItemRepository;

        public MessageQueueService(
            IGenericRepository<QueueItem> queueItemRepository)
        {
            this.queueItemRepository = queueItemRepository;
        }

        /// <summary>
        /// Inserts an activity invocation into the message queue. 
        /// </summary>
        /// <param name="zombie">The ZombieClient for which to invoke the activity</param>
        /// <param name="activity">The activity to invoke</param>
        /// <param name="parameters">The parameters which to pass to the activity</param>
        /// <param name="commandName">Todo .</param>
        /// <returns>The queue item ticket</returns>
        public Guid InsertActivityInvocation(Zombie zombie, Activity activity, Dictionary<string, string> parameters, string commandName)
        {
            var queueItem = new ActivityInvocationQueueItem
                {
                    Activity = activity,
                    Reciever = zombie,
                    TimeOut = 20,
                    Parameters = parameters,
                    CommandName = commandName,
                    RecievedAtCloud = DateTime.UtcNow
                };

            queueItemRepository.Add(queueItem);

            ProcessQueueItem(queueItem);
            return queueItem.Ticket;
        }

        /// <summary>
        /// Marks the QueueItem as delivered
        /// </summary>
        /// <param name="ticket">The ticket of the QueueItem which to mark as delivered</param>
        public void MarkQueueItemAsDelivered(Guid ticket)
        {
            var queueItem = queueItemRepository.Get(ticket);

            queueItem.Delivered = DateTime.UtcNow;
            queueItemRepository.Update(queueItem);
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
            }
        }

        [ExcludeFromCodeCoverage]
        private void SendActivityInvocation(ActivityInvocationQueueItem item)
        {
            string connectionId = item.Reciever.ConnectionId;
            GlobalHost.ConnectionManager.GetHubContext<ZombieHub>().Clients.Client(connectionId)
                .InvokeActivity(item.Activity.Id, item.Ticket, item.Parameters);
        }
    }

}

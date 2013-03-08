using System;
using System.Collections.Generic;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using SignalR;

namespace Controll.Hosting.Services
{
    public sealed class MessageQueueService : IMessageQueueService
    {
        private readonly IGenericRepository<ActivityInvocationQueueItem> activityInvocationQueueItemRepository;
        private readonly IGenericRepository<ActivityDownloadOrderQueueItem> activityDownloadOrderQueueItem;
        private readonly IGenericRepository<QueueItem> queueItemRepository;

        public MessageQueueService(
            IGenericRepository<ActivityInvocationQueueItem> activityInvocationQueueItemRepository,
            IGenericRepository<ActivityDownloadOrderQueueItem> activityDownloadOrderQueueItem,
            IGenericRepository<QueueItem> queueItemRepository)
        {
            this.activityInvocationQueueItemRepository = activityInvocationQueueItemRepository;
            this.activityDownloadOrderQueueItem = activityDownloadOrderQueueItem;
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

            activityInvocationQueueItemRepository.Add(queueItem);

            ProcessQueueItem(queueItem);
            return queueItem.Ticket;
        }

        /// <summary>
        /// Inserts an zombie-order to download a new activity in the queue
        /// </summary>
        /// <param name="zombie">The zombie which will download the activity</param>
        /// <param name="activity">The activity</param>
        /// <returns>The queue item ticket</returns>
        public Guid InsertActivityDownloadOrder(Zombie zombie, Activity activity)
        {
            var queueItem = new ActivityDownloadOrderQueueItem
            {
                Activity = activity,
                Reciever = zombie,
                TimeOut = 20,
                RecievedAtCloud = DateTime.UtcNow
            };

            activityDownloadOrderQueueItem.Add(queueItem);

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

        public QueueItem GetQueueItem(Guid ticket)
        {
            return queueItemRepository.Get(ticket);
        }

        public IList<ActivityInvocationQueueItem> GetFinishedActivityInvocationLogItems(ControllUser forUser, int maxResult)
        {
            Console.WriteLine("WARNING: FIX GetFinishedActivityInvocationLogItems TO FILTER ON USER");
            return activityInvocationQueueItemRepository.GetAll(maxResult);
        }

        private void ProcessQueueItem(QueueItem queueItem)
        {
            if (string.IsNullOrEmpty(queueItem.Reciever.ConnectionId))
                return;

            var type = queueItem.Type;
            switch (type)
            {
                case QueueItemType.DownloadOrder:
                    SendDownloadOrder((ActivityDownloadOrderQueueItem)queueItem);
                    break;
                case QueueItemType.ActivityInvocation:
                    SendActivityInvocation((ActivityInvocationQueueItem)queueItem);
                    break;
            }
        }

        private void SendActivityInvocation(ActivityInvocationQueueItem item)
        {
            string connectionId = item.Reciever.ConnectionId;
            GlobalHost.ConnectionManager.GetHubContext<ZombieHub>().Clients[connectionId]
                .InvokeActivity(item.Activity.Id, item.Ticket, item.Parameters);
        }

        private void SendDownloadOrder(ActivityDownloadOrderQueueItem item)
        {
            string connectionId = item.Reciever.ConnectionId;
            GlobalHost.ConnectionManager.GetHubContext<ZombieHub>().Clients[connectionId]
                .ActivityDownloadOrder(item.Activity.Id, item.Ticket);
        }
    }

}

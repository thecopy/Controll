using System;
using System.Collections.Generic;
using Controll.Hosting.Models;

namespace Controll.Hosting.Services
{
    public interface IMessageQueueService
    {
        /// <summary>
        /// Inserts an activity invocation into the message queue. 
        /// </summary>
        /// <param name="zombie">The ZombieClient for which to invoke the activity</param>
        /// <param name="activity">The activity to invoke</param>
        /// <param name="parameters">The parameters which to pass to the activity</param>
        /// <returns>The queue item ticket</returns>
        Guid InsertActivityInvocation(Zombie zombie, Activity activity, Dictionary<string, string> parameters, string commandName);

        /// <summary>
        /// Inserts an zombie-order to download a new activity
        /// </summary>
        /// <param name="zombie">The zombie which will download the activity</param>
        /// <param name="activity">The activity</param>
        /// <returns>The queue item ticket</returns>
        Guid InsertActivityDownloadOrder(Zombie zombie, Activity activity);

        /// <summary>
        /// Marks the QueueItem as delivered
        /// </summary>
        /// <param name="ticket">The ticket of the QueueItem which to mark as delivered</param>
        void MarkQueueItemAsDelivered(Guid ticket);

        QueueItem GetQueueItem(Guid ticket);
        IList<ActivityInvocationQueueItem> GetFinishedActivityInvocationLogItems(ControllUser forUser, int maxResult);
    }
}
using System;
using System.Collections.Generic;
using Controll.Common;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Controll.Hosting.Repositories;
using NHibernate;

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
        /// <param name="connectionId">The connection-id of the initiating client</param>
        /// <returns>The queue item ticket</returns>
        QueueItem InsertActivityInvocation(Zombie zombie, Activity activity, IDictionary<string, string> parameters, string commandName, string connectionId);

        /// <summary>
        /// Marks the QueueItem as delivered
        /// </summary>
        /// <param name="ticket">The ticket of the QueueItem which to mark as delivered</param>
        void MarkQueueItemAsDelivered(Guid ticket);


        /// <summary>
        /// Inserts a ping message to a zombie
        /// </summary>
        /// <returns>The ping items queue ticket</returns>
        QueueItem InsertPingMessage(Zombie zombie, string senderConnectionId);

        void ProcessUndeliveredMessagesForZombie(Zombie zombie);

        void InsertActivityMessage(Guid ticket, ActivityMessageType type, string message);

        void InsertActivityResult(Guid ticket, ActivityCommand result);

        void ProcessQueueItem<T>(T queueItem) where T : QueueItem;
    }
}
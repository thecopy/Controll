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
        private readonly IDispatcher _dispatcher;

        public ControllService(ISession session, IControllRepository controllRepository, IDispatcher dispatcher)
        {
            _session = session;
            _controllRepository = controllRepository;
            _dispatcher = dispatcher;
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
            {
                var id = connectionId;
                _dispatcher.ManualClientMessage(clients =>
                                                clients.Client(id).MessageDelivered(ticket));
            }
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
                _dispatcher.Dispatch(queueItem);
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

                var id = connectionId;
                _dispatcher.ManualClientMessage(clients => clients.Client(id).ActivityMessage(ticket, type, message));

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

            _dispatcher.Dispatch(activityResultQueueItem);
        }

        
    }
}

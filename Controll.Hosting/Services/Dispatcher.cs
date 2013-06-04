using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Helpers;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Controll.Hosting.Services
{
    public class Dispatcher : IDispatcher
    {
        private readonly IConnectionManager _connectionManager;

        private IHubConnectionContext ClientHub { get { return _connectionManager.GetHubContext<ClientHub>().Clients; } }
        private IHubConnectionContext ZombieHub { get { return _connectionManager.GetHubContext<ZombieHub>().Clients; } }

        public Dispatcher(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public void ClientMessage(Action<IHubConnectionContext> action)
        {
            action.Invoke(ClientHub);
        }

        public void ZombieMessage(Action<IHubConnectionContext> action)
        {
            action.Invoke(ZombieHub);
        }

        public void Dispatch(QueueItem queueItem)
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

        public void SendActivitiesSynchronizedMessage(Zombie zombie)
        {
            foreach (var connectionId in zombie.Owner.ConnectedClients.Select(x => x.ConnectionId))
            {
                ClientHub.Client(connectionId)
                    .ZombieSynchronized(zombie.Name, zombie.Activities.Select(x => x.CreateViewModel()));
            }
        }

        private void SendDownloadActivity(DownloadActivityQueueItem qi, string connectionId)
        {
            ZombieHub.Client(connectionId)
                              .DownloadActivity(qi.Ticket, qi.Url);
        }

        private void SendActivityResult(ActivityResultQueueItem queueItem, string connectionId)
        {
            ClientHub.Client(connectionId)
                              .ActivityResult(queueItem.InvocationTicket, queueItem.ActivityCommand.CreateViewModel());
        }

        private void SendPing(PingQueueItem item, string connectionId)
        {
            ZombieHub.Client(connectionId)
                              .Ping(item.Ticket);
        }

        private void SendActivityInvocation(ActivityInvocationQueueItem item, string connectionId)
        {
            ZombieHub.Client(connectionId)
                              .InvokeActivity(item.Activity.Id, item.Ticket, item.Parameters, item.CommandName);
        }
    }
}

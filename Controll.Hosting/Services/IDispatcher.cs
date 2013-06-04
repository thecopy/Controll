using System;
using Controll.Hosting.Models.Queue;
using Microsoft.AspNet.SignalR.Hubs;

namespace Controll.Hosting.Services
{
    public interface IDispatcher
    {
        void Dispatch(QueueItem queueItem);
        void ClientMessage(Action<IHubConnectionContext> action);
        void ZombieMessage(Action<IHubConnectionContext> action);
    }
}
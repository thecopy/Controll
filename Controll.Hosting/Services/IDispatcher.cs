using System;
using Controll.Hosting.Models.Queue;
using Microsoft.AspNet.SignalR.Hubs;

namespace Controll.Hosting.Services
{
    public interface IDispatcher
    {
        void Dispatch(QueueItem queueItem);
        void ManualClientMessage(Action<IHubConnectionContext> action);
        void ManualZombieMessage(Action<IHubConnectionContext> action);
    }
}
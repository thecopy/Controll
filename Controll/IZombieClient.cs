using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Zombie.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll.Client
{
    public interface IZombieClient: IActivityDelegator
    {
        event Action<Guid, string> DownloadActivityRequest;
        event Action<InvocationInformation> InvocationRequest;
        event Action<Guid> Pinged;

        string Url { get; }
        HubConnection HubConnection { get; }

        Task Connect(string username, string password, string zombieName);
        Task Synchronize(IEnumerable<ActivityViewModel> activities);
        Task ConfirmMessageDelivery(Guid ticket);
    }
}

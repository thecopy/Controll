using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Zombie.Infrastructure;

namespace Controll.Zombie
{
    public interface IZombieClient: IActivityDelegator
    {
        event Action<InvocationInformation> InvocationRequest;
        event Action<Guid, String> ActivityCompleted;
        event Action<Guid> Pinged;

        Task Connect(string username, string password, string zombieName);
        Task Synchronize(IEnumerable<ActivityViewModel> activities);
        Task ConfirmMessageDelivery(Guid ticket);
    }
}

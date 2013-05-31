using System.Collections.Generic;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;

namespace Controll.Hosting.Repositories
{
    public interface IControllRepository
    {
        ControllClient GetClientByConnectionId(string connectionId);
        T GetClientByConnectionId<T>(string connectionId) where T:ClientCommunicator;

        ControllUser GetUserFromUserName(string username);
        ControllUser GetUserFromEmail(string email);

        IList<QueueItem> GetUndeliveredQueueItemsForZombie(int zombieId);
    }
}
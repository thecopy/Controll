using System;
using Controll.Common;
using Controll.Hosting.Models;

namespace Controll.Hosting.Services
{
    public interface IActivityService
    {
        event EventHandler<Tuple<Guid, ActivityInvocationLogMessage>> NewActivityLogItem;
        void OnNewActivityLogItem(Guid ticket, ActivityInvocationLogMessage item);
        void UpdateLogWithResponse(Guid ticket, string response);
        void InsertActivityLogMessage(Guid ticket, ActivityMessageType type, string message);
        void AddActivityToZombie(string zombieName, ControllUser user, Guid key);
        Guid GetLatestStartedActivity(ControllUser user, Zombie zombie, Guid guid);
        byte[] GetActivityBinaryData(Guid activityKey);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;

namespace Controll.Hosting.Services
{
    public interface IControllService
    {
        QueueItem InsertActivityInvocation(Zombie zombie, Activity activity, IDictionary<string, string> parameters, string commandName, string connectionId);
        QueueItem InsertPingMessage(Zombie zombie, string senderConnectionId);
        void InsertActivityLogMessage(Guid ticket, ActivityMessageType type, string message);
        void InsertActivityMessage(Guid ticket, ActivityMessageType type, string message);
        void InsertActivityResult(Guid ticket, ActivityCommand result);

        void MarkQueueItemAsDelivered(Guid ticket);
        
        void ProcessUndeliveredMessagesForZombie(Zombie zombie);
        void ProcessQueueItem<T>(T queueItem) where T : QueueItem;
        
        ICollection<ActivityInvocationLogMessage> GetActivityMessagesForInvocationTicket(Guid ticket);
        ActivityInvocationQueueItem GetActivityInvocationFromTicket(Guid ticket);
        IList<ActivityInvocationLogBookViewModel> GetActivityLog(Zombie zombie);
    }
}

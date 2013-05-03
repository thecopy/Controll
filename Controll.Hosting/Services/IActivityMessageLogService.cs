using System;
using System.Collections.Generic;
using Controll.Common;
using Controll.Hosting.Models;

namespace Controll.Hosting.Services
{
    public interface IActivityMessageLogService
    {
        void InsertActivityLogMessage(Guid ticket, ActivityMessageType type, string message);
        ICollection<ActivityInvocationLogMessage> GetActivityMessagesForInvocationTicket(Guid ticket);
        ActivityInvocationQueueItem GetActivityInvocationFromTicket(Guid ticket);
    }
}
using System;
using System.Collections.Generic;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Models;
using ActivityInvocationLogMessage = Controll.Hosting.Models.ActivityInvocationLogMessage;

namespace Controll.Hosting.Services
{
    public interface IActivityMessageLogService
    {
        void InsertActivityLogMessage(Guid ticket, ActivityMessageType type, string message);

        ICollection<ActivityInvocationLogMessage> GetActivityMessagesForInvocationTicket(Guid ticket);

        ActivityInvocationQueueItem GetActivityInvocationFromTicket(Guid ticket);

        /// <summary>
        /// Gets all activity messages for the zombie.
        /// </summary>
        /// <param name="zombie">The zombie</param>
        /// <returns></returns>
        IList<ActivityInvocationLogBookViewModel> GetActivityLog(Zombie zombie);

    }
}
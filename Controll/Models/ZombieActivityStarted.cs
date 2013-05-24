using System;
using System.Collections.Generic;

namespace Controll.Client.Models
{
    public class ZombieActivityStarted : EventArgs
    {
        public Guid ActivityId { get; private set; }
        public Guid ActivityTicket { get; private set; }
        public Dictionary<string, string> Parameter { get; set; }

        public ZombieActivityStarted(Guid activityId, Guid activityTicket, Dictionary<string, string> parameters)
            : base()
        {
            ActivityId = activityId;
            ActivityTicket = activityTicket;
            Parameter = parameters;
        }
    }
}

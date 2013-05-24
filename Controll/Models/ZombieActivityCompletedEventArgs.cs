using System;

namespace Controll.Client.Models
{
    public class ZombieActivityCompletedEventArgs : EventArgs
    {
        public Guid ActivityTicket { get; private set; }
        public object Result { get; private set; }

        public ZombieActivityCompletedEventArgs(Guid activityTicket, object result)
            : base()
        {
            Result = result;
            ActivityTicket = activityTicket;
        }
    }
}

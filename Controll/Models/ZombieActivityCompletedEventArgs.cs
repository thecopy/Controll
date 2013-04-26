using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll
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

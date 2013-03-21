using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll
{
    public class MessageDeliveredEventArgs : EventArgs
    {
        public Guid DeliveredTicket { get; private set; }

        public MessageDeliveredEventArgs(Guid deliveredTicket)
            : base()
        {
            DeliveredTicket = deliveredTicket;
        }
    }
}

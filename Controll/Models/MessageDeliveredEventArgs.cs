using System;

namespace Controll.Client.Models
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

using System;

namespace Controll.Hosting.Models.Queue
{
    public enum QueueItemType
    {
        ActivityInvocation,
        Ping,
        DeliveryAcknowledgement,
        ActivityResult
    }

    public abstract class QueueItem
    {
        public virtual Guid Ticket { get; set; }
        public virtual ClientCommunicator Reciever { get; set; }
        public virtual ClientCommunicator Sender { get; set; }
        public virtual DateTime RecievedAtCloud { get; set; }
        public virtual DateTime? Delivered { get; set; }
        public abstract QueueItemType Type { get; }

    }
}

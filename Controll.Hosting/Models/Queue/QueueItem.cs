using System;

namespace Controll.Hosting.Models.Queue
{
    public enum QueueItemType
    {
        ActivityInvocation,
        Ping,
        DeliveryAcknowledgement
    }

    public abstract class QueueItem
    {
        public virtual Guid Ticket { get; set; }
        public virtual Zombie Reciever { get; set; }
        public virtual DateTime RecievedAtCloud { get; set; }
        public virtual DateTime? Delivered { get; set; }
        public virtual int TimeOut { get; set; }
        public abstract QueueItemType Type { get; }

        public virtual string SenderConnectionId { get; set; }
    }
}

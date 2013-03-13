using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public enum QueueItemType
    {
        ActivityInvocation
    }

    public abstract class QueueItem
    {
        public virtual Guid Ticket { get; set; }
        public virtual Zombie Reciever { get; set; }
        public virtual ControllUser Owner { get; set; }
        public virtual DateTime RecievedAtCloud { get; set; }
        public virtual DateTime? Delivered { get; set; }
        public virtual int TimeOut { get; set; }
        public abstract QueueItemType Type { get; }
    }
}

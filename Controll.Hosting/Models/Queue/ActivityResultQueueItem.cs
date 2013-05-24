using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common.ViewModels;

namespace Controll.Hosting.Models.Queue
{
    public class ActivityResultQueueItem : QueueItem
    {
        public virtual ActivityCommand ActivityCommand { get; set; }
        public virtual Guid InvocationTicket { get; set; }

        public override QueueItemType Type
        {
            get { return QueueItemType.ActivityResult; }
        }
    }
}

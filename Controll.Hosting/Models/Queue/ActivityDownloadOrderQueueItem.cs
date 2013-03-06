using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public class ActivityDownloadOrderQueueItem : QueueItem
    {
        public virtual Activity Activity { get; set; }

        public override QueueItemType Type
        {
            get { return QueueItemType.DownloadOrder; }
        }
    }
}

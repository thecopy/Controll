using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll.Hosting.Models
{
    public class ActivityInvocationQueueItem : QueueItem
    {
        public virtual Activity Activity { get; set; }
        public virtual IDictionary<string, string> Parameters { get; set; }
        public virtual string CommandName { get; set; }

        public virtual IList<ActivityInvocationLogMessage> MessageLog { get; set; }
        public virtual string Response { get; set; }
        public virtual DateTime? Responded { get; set; }

        public override QueueItemType Type
        {
            get { return QueueItemType.ActivityInvocation; }
        }
    }
}

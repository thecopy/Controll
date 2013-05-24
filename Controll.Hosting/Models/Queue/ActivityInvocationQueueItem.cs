using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models.Queue;

namespace Controll.Hosting.Models
{
    public class ActivityInvocationQueueItem : QueueItem
    {
        private IList<ActivityInvocationLogMessage> _messageLog = new List<ActivityInvocationLogMessage>();

        public virtual Activity Activity { get; set; }
        public virtual IDictionary<string, string> Parameters { get; set; }
        public virtual string CommandName { get; set; }

        public virtual IList<ActivityInvocationLogMessage> MessageLog
        {
            get { return _messageLog; }
            set { _messageLog = value; }
        }

        public virtual string Response { get; set; }
        public virtual DateTime? Responded { get; set; }

        public override QueueItemType Type
        {
            get { return QueueItemType.ActivityInvocation; }
        }
    }
}

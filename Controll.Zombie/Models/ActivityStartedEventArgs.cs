using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll
{
    public class ActivityStartedEventArgs : EventArgs
    {
        public ActivityStartedEventArgs(Guid activityKey, Guid activityTicket, IDictionary<string, string> parameter)
        {
            Parameter = parameter;
            ActivityTicket = activityTicket;
            ActivityKey = activityKey;
        }

        public Guid ActivityKey { get; private set; }
        public Guid ActivityTicket { get; private set; }
        public IDictionary<string, string> Parameter { get; private set; }
    }
}

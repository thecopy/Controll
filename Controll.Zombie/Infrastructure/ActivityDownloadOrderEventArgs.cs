using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll
{
    public class ActivityDownloadOrderEventArgs : EventArgs
    {
        public Guid ActivityKey { get; set; }
        public Guid Ticket { get; set; }

        public ActivityDownloadOrderEventArgs(Guid activityKey, Guid ticket)
        {
            ActivityKey = activityKey;
            Ticket = ticket;
        }
    }
}

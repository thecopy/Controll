using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll
{
    public  class ActivityLogMessageEventArgs : EventArgs
    {
        public Guid Ticket { get; set; }
        public ActivityMessageType Type { get; set; }
        public string Message { get; set; }

        public ActivityLogMessageEventArgs(Guid ticket, string message, ActivityMessageType type)
        {
            Ticket = ticket;
            Type = type;
            Message = message;
        }
    }
}

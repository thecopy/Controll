using System;
using Controll.Common;

namespace Controll.Client.Models
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

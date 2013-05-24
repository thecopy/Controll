using System;

namespace Controll.Client.Models
{
    public  class ActivityResultEventArgs : EventArgs
    {
        public Guid Ticket { get; set; }
        public object Result { get; set; }

        public ActivityResultEventArgs(Guid ticket, object result)
        {
            Ticket = ticket;
            Result = result;
        }
    }
}

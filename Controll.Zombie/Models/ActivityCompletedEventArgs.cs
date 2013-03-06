using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll
{
    public class ActivityCompletedEventArgs : EventArgs
    {
        public Guid Ticket { get; set; }
        public string Result { get; set; }

        public ActivityCompletedEventArgs(Guid ticket, string result)
        {
            Ticket = ticket;
            Result = result;
        }
    }
}

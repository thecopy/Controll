using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll
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

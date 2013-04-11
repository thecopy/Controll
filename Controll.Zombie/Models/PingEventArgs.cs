using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll
{
    public class PingEventArgs : EventArgs
    {
        public Guid Ticket { get; private set; }

        public PingEventArgs(Guid ticket)
        {
            Ticket = ticket;
        }
    }
}

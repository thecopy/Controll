using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll
{
    public class ActivityDownloadedAtZombieEventArgs : EventArgs
    {
        public Guid Ticket { get; set; }

        public ActivityDownloadedAtZombieEventArgs(Guid ticket)
        {
            Ticket = ticket;
        }
    }
}

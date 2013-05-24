using System;

namespace Controll.Client.Models
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

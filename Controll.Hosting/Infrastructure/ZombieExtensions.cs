using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public static class ZombieExtensions
    {
        internal static bool IsOnline(this Zombie zombie)
        {
            return zombie.ConnectedClients != null && zombie.ConnectedClients.Any(x => x.ConnectionId != null);
        }

        internal static Activity GetActivity(this Zombie zombie, Guid key)
        {
            return zombie.Activities.FirstOrDefault(a => a.Id == key);
        }
    }
}

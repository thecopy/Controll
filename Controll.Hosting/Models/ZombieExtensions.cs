using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public static class ZombieExtensions
    {
        public static bool IsOnline(this Zombie zombie)
        {
            return !string.IsNullOrEmpty(zombie.ConnectionId);
        }

        public static Activity GetActivity(this Zombie zombie, Guid key)
        {
            return zombie.Activities.FirstOrDefault(a => a.Id == key);
        }
    }
}

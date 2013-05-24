using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public static class ControllUserExtensions
    {
        internal static Zombie GetZombieByName(this ControllUser user, string zombieName)
        {
            return user.Zombies != null
                       ? user.Zombies.SingleOrDefault(z => z.Name == zombieName)
                       : null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Infrastructure
{
    public static class Constants
    {
        public const String ControllAuthType = "Controll";
    }

    public static class ControllClaimTypes
    {
        public const String UserIdentifier = "urn:controll:userId";
        public const String ZombieIdentifier = "urn:controll:zombieId";
    }
}

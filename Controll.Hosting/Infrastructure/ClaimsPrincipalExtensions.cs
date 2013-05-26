using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Infrastructure
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetClaim(this ClaimsPrincipal self, string claimType)
        {
            return self.Claims.Single(x => x.Type == claimType).Value;
        }
        public static bool HasClaim(this ClaimsPrincipal self, params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                if (self.Claims.Any(x => x.Type == claimType))
                    return true;
            }

            return false;
        }
    }
}

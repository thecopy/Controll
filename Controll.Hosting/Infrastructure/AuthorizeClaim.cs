using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.SignalR;

namespace Controll.Hosting.Infrastructure
{
    public class AuthorizeClaim : AuthorizeAttribute
    {
        private readonly string[] _claimTypes;
        public AuthorizeClaim(params string[] claimTypes)
        {
            _claimTypes = claimTypes;
        }

        protected override bool UserAuthorized(IPrincipal user)
        {
            var claimsPrincipal = user as ClaimsPrincipal;

            return claimsPrincipal != null && claimsPrincipal.HasClaim(_claimTypes);
        }
    }

}

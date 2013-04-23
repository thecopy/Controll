using Microsoft.AspNet.SignalR;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    public class BaseHub : Hub
    {
        protected readonly ISession Session;

        public BaseHub(ISession session)
        {
            Session = session;
        }
    }
}

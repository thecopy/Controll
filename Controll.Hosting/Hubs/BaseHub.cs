using Microsoft.AspNet.SignalR;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    public class BaseHub : Hub
    {
        public ISession Session { get; private set; }

        public BaseHub(ISession session)
        {
            Session = session;
        }
    }
}

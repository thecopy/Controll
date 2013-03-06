using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll
{
    class DelegatePluginContext : IPluginContext
    {
        private readonly Guid ticket;
        private readonly Action<Guid, string> finished;
        private readonly Action<Guid> started;
        private readonly Action<Guid, string> error;
        private readonly Action<Guid, string> notify;

        public DelegatePluginContext(Guid ticket, IDictionary<string, string> parameters, IControllPluginClient client)
        {
            this.ticket = ticket;
            this.Parameters = parameters;
            this.finished = client.ActivityCompleted;
            this.error = client.ActivityError;
            this.notify = client.ActivityNotify;
            this.started = client.ActivityStarted;
        }

        public IDictionary<string, string> Parameters { get; private set; }
        public object[] Arguments { get; private set; }

        public void Started()
        {
            started.Invoke(ticket);
        }

        public void Finish(string result)
        {
            finished.Invoke(ticket, result);
        }

        public void Error(string errorMessage)
        {
            error.Invoke(ticket, errorMessage);
        }

        public void Notify(string message)
        {
            notify.Invoke(ticket, message);
        }
    }
}

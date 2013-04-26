using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll
{
    class DelegateActivityContext : IActivityContext
    {
        private readonly Guid _ticket;
        private readonly Action<Guid, string> _finished;
        private readonly Action<Guid> _started;
        private readonly Action<Guid, string> _error;
        private readonly Action<Guid, string> _notify;
        private readonly Action<Guid, object> _result;

        public DelegateActivityContext(Guid ticket, IDictionary<string, string> parameters, IActivityDelegator client)
        {
            this._ticket = ticket;
            this.Parameters = parameters;
            this._finished = client.ActivityCompleted;
            this._error = client.ActivityError;
            this._notify = client.ActivityNotify;
            this._started = client.ActivityStarted;
            this._result = client.ActivityResult;
        }

        public IDictionary<string, string> Parameters { get; private set; }
        public object[] Arguments { get; private set; }

        public void Started()
        {
            _started.Invoke(_ticket);
        }

        public void Finish(string result)
        {
            _finished.Invoke(_ticket, result);
        }

        public void Error(string errorMessage)
        {
            _error.Invoke(_ticket, errorMessage);
        }

        public void Notify(string message)
        {
            _notify.Invoke(_ticket, message);
        }

        public void Result(object result)
        {
            _result.Invoke(_ticket, result);
        }
    }
}

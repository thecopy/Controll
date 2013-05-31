using System;
using System.Collections.Generic;
using Controll.Common;

namespace Controll.Zombie.Infrastructure
{
    class DelegateActivityContext : IActivityContext
    {
        private readonly Guid _ticket;
        private readonly Action<Guid, string> _finished;
        private readonly Action<Guid> _started;
        private readonly Action<Guid, string> _error;
        private readonly Action<Guid, string> _notify;
        private readonly Action<Guid, object> _result;

        public DelegateActivityContext(Guid ticket, IDictionary<string, string> parameters, string commandName, IActivityDelegator client)
        {
            Parameters = parameters;
            CommandName = commandName;

            _ticket = ticket;
            _finished = client.ActivityCompletedMessage;
            _error = client.ActivityError;
            _notify = client.ActivityNotify;
            _started = client.ActivityStarted;
            _result = client.ActivityResult;
        }

        public IDictionary<string, string> Parameters { get; private set; }
        public string CommandName { get; set; }
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

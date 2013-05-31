using System;
using System.Collections.Generic;
using Controll.Common;

namespace Controll.Zombie.Infrastructure
{
    class DelegateActivityContext : IActivityContext
    {
        private readonly Guid _ticket;
        private readonly Action<Guid, ActivityMessageType, string> _notify;
        private readonly Action<Guid, object> _result;

        public DelegateActivityContext(Guid ticket, IDictionary<string, string> parameters, string commandName, IActivityDelegator client)
        {
            Parameters = parameters;
            CommandName = commandName;

            _ticket = ticket;

            _notify = client.ActivityMessage;
            _result = client.ActivityResult;
        }

        public IDictionary<string, string> Parameters { get; private set; }
        public string CommandName { get; set; }

        public void Message(ActivityMessageType type, string message)
        {
            _notify.Invoke(_ticket, type, message);
        }

        public void Result(object result)
        {
            _result.Invoke(_ticket, result);
        }
    }
}

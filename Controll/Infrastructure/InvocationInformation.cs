using System;
using System.Collections.Generic;

namespace Controll.Zombie.Infrastructure
{
    public class InvocationInformation
    {
        public InvocationInformation(Guid activityKey, Guid ticket, IDictionary<string, string> parameter, string commandName)
        {
            Parameter = parameter;
            CommandName = commandName;
            Ticket = ticket;
            ActivityKey = activityKey;
        }

        public Guid ActivityKey { get; private set; }
        public Guid Ticket { get; private set; }
        public IDictionary<string, string> Parameter { get; private set; }
        public string CommandName { get; set; }
    }
}

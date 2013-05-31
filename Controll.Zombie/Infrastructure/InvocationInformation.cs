using System;
using System.Collections.Generic;

namespace Controll.Zombie.Infrastructure
{
    public class InvocationInformation
    {
        public InvocationInformation(Guid activityKey, Guid activityTicket, IDictionary<string, string> parameter, string commandName)
        {
            Parameter = parameter;
            CommandName = commandName;
            ActivityTicket = activityTicket;
            ActivityKey = activityKey;
        }

        public Guid ActivityKey { get; private set; }
        public Guid ActivityTicket { get; private set; }
        public IDictionary<string, string> Parameter { get; private set; }
        public string CommandName { get; set; }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Common.ViewModels
{
    public class LogBookViewModel
    {
        public Guid InvocationTicket { get; set; }

        public string ActivityName { get; set; }
        public string CommandLabel { get; set; }

        public DateTime? Delivered { get; set; }
        public DateTime? Started
        {
            get
            {
                if (Messages.All(msg => msg.MessageType != ActivityMessageType.Started))
                    return null;

                return Messages
                    .First(msg => msg.MessageType == ActivityMessageType.Started)
                    .Date;
            }
        }
        public DateTime? Finished
        {
            get
            {
                if (!Messages.Any(x => x.MessageType == ActivityMessageType.Completed || x.MessageType == ActivityMessageType.Failed))
                    return null;

                return Messages
                    .First(msg => msg.MessageType == ActivityMessageType.Completed || msg.MessageType == ActivityMessageType.Failed)
                    .Date;
            }
        }

        public IEnumerable<LogMessageViewModel> Messages { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}

﻿using System;
using Controll.Common;

namespace Controll.Hosting.Models
{
    public class LogMessage
    {
        public virtual int Id { get; set; }

        public virtual ActivityMessageType Type { get; set; }
        public virtual string Message { get; set; }
        public virtual DateTime Date { get; set; }
    }
}

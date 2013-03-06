using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll.Hosting.Models
{
    public class ActivityInvocationLogMessage
    {
        public virtual ActivityMessageType Type { get; set; }
        public virtual string Message { get; set; }
        public virtual DateTime Date { get; set; }

    }
}

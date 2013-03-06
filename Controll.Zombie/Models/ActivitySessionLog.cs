using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll
{
    public class ActivitySessionLog
    {
        public virtual Guid Ticket { get; set; }
        public virtual ActivityViewModel Activity { get; set; }
        public virtual DateTime Started { get; set; }
        public virtual DateTime? Completed { get; set; }
        public virtual IDictionary<string, string> Parameters { get; set; } 
    }
}

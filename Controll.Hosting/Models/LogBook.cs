using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public class LogBook
    {
        public virtual int Id { get; set; }

        public virtual IList<LogMessage> LogMessages { get; set; }
        public virtual Activity Activity { get; set; }
        public virtual Guid InvocationTicket { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll.Hosting.Models
{
    public class ControllClient
    {
        public virtual int Id { get; set; }

        public virtual string ConnectionId { get; set; }
        public virtual ClientCommunicator ClientCommunicator { get; set; }
    }
}

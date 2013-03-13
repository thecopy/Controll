using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public class ControllUser
    {
        public virtual int Id { get; set; }
        public virtual string UserName { get; set; }
        public virtual string Password { get; set; }
        public virtual string EMail { get; set; }
        public virtual IList<Device> Devices { get; set; }
        public virtual IList<ControllClient> ConnectedClients { get; set; }
        public virtual IList<Zombie> Zombies { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Models
{
    public class ControllUser : ClientCommunicator
    {
        private IList<Zombie> _zombies = new List<Zombie>();
        public virtual string UserName { get; set; }
        public virtual string Password { get; set; }
        public virtual string Email { get; set; }
        public virtual IList<Zombie> Zombies
        {
            get { return _zombies; }
            set { _zombies = value; }
        }
    }
}

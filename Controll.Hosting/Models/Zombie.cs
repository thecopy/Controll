using System.Collections.Generic;
using Controll.Common.ViewModels;

namespace Controll.Hosting.Models
{
    public class Zombie : ClientCommunicator
    {
        public virtual string Name { get; set; }
        public virtual IList<Activity> Activities { get; set; }
        public virtual ControllUser Owner { get; set; }
    }
}

using System.Collections.Generic;
using Controll.Common.ViewModels;

namespace Controll.Hosting.Models
{
    public class Zombie : ClientCommunicator
    {
        private IList<Activity> _activities = new List<Activity>();
        public virtual string Name { get; set; }
        public virtual IList<Activity> Activities   
        {
            get { return _activities; }
            set { _activities = value; }
        }

        public virtual ControllUser Owner { get; set; }
    }
}

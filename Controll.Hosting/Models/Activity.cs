using System;
using System.Collections.Generic;
using Controll.Common;

namespace Controll.Hosting.Models
{
    public class Activity
    {
        private IList<ActivityCommand> _commands = new List<ActivityCommand>();

        public virtual Guid Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string CreatorName { get; set; }
        public virtual string Description { get; set; }
        public virtual DateTime LastUpdated { get; set; }
        public virtual IList<ActivityCommand> Commands  
        {
            get { return _commands; }
            set { _commands = value; }
        }

        public virtual Version Version { get; set; }
    }
}

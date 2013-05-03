using System;
using System.Collections.Generic;
using Controll.Common;

namespace Controll.Hosting.Models
{
    public class Activity
    {
        public virtual Guid Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string CreatorName { get; set; }
        public virtual string Description { get; set; }
        public virtual DateTime LastUpdated { get; set; }
        public virtual IList<ActivityCommand> Commands { get; set; }
        public virtual Version Version { get; set; }
    }
}

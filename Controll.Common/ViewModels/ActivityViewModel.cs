using System;
using System.Collections.Generic;

namespace Controll.Common.ViewModels
{
    public class ActivityViewModel
    {
        public virtual string Name { get; set; }
        public virtual string CreatorName { get; set; }
        public virtual Version Version { get; set; }
        public virtual DateTime LastUpdated { get; set; }
        public virtual string Description { get; set; }
        public virtual Guid Key { get; set; }
        public IEnumerable<ActivityCommandViewModel> Commands { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Common
{
    public class ActivityViewModel
    {
        public virtual string Name { get; set; }
        public virtual string CreatorName { get; set; }
        public virtual Version Version { get; set; }
        public virtual DateTime LastUpdated { get; set; }
        public virtual string Description { get; set; }
        public virtual Guid Key { get; set; }

        public static ActivityViewModel CreateFrom(Activity activity)
        {
            var vm = new ActivityViewModel
                {
                    Key = activity.Id,
                    Name = activity.Name,
                    CreatorName = activity.CreatorName,
                    Version = activity.Version,
                    Description = activity.Description,
                    LastUpdated = activity.LastUpdated
                };

            return vm;
        }
    }
}

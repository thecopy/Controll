using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    public class ActivitytResultQueueItemMap : SubclassMap<ActivityResultQueueItem>
    {
        public ActivitytResultQueueItemMap()
        {
            References(x => x.ActivityCommand).Cascade.All();
            Map(x => x.InvocationTicket).Not.Nullable();
        }
    }
}

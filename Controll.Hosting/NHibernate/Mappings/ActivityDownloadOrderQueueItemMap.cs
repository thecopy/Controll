using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    public class ActivityDownloadOrderQueueItemMap : SubclassMap<ActivityDownloadOrderQueueItem>
    {
        public ActivityDownloadOrderQueueItemMap()
        {
            References(x => x.Activity);
        }
    }
}

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
    internal class DownloadActivityQueueItemMap : SubclassMap<DownloadActivityQueueItem>
    {
        internal DownloadActivityQueueItemMap()
        {
            Map(x => x.Url);
        }
    }
}

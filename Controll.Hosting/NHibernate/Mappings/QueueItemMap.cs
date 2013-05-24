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
    internal class QueueItemMap : ClassMap<QueueItem>
    {
        internal QueueItemMap()
        {
            Id(x => x.Ticket);
            Map(x => x.Delivered);
            Map(x => x.RecievedAtCloud);
            References(x => x.Reciever).Not.Nullable();
            References(x => x.Sender).Not.Nullable();
        }
    }
}

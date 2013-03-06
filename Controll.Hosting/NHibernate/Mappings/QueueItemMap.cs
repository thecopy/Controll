using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    public class QueueItemMap : ClassMap<QueueItem>
    {
        public QueueItemMap()
        {
            Id(x => x.Ticket);
            Map(x => x.Delivered);
            References(x => x.Owner).Not.LazyLoad();
            Map(x => x.RecievedAtCloud);
            References(x => x.Reciever).Not.LazyLoad();
            Map(x => x.TimeOut);
        }
    }
}

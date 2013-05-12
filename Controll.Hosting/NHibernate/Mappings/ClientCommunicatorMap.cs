using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    public class ClientCommunicatorMap : ClassMap<ClientCommunicator>
    {
        public ClientCommunicatorMap()
        {
            Id(x => x.Id);
            HasMany(x => x.ConnectedClients)
                .Component(c => c.Map(x => x.ConnectionId))
                .KeyColumn("Id")
                .Cascade.AllDeleteOrphan();
        }
    }
}

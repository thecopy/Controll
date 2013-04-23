using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    public class ZombieMap : ClassMap<Zombie>
    {
        public ZombieMap()
        {
            Id(x => x.Id);
            Map(x => x.ConnectionId);
            Map(x => x.Name);
            HasManyToMany(x => x.Activities).Not.LazyLoad().Cascade.SaveUpdate();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    internal class ZombieMap : SubclassMap<Zombie>
    {
        internal ZombieMap()
        {
            Map(x => x.Name).Not.Nullable();
            References(x => x.Owner).Not.Nullable().Column("Owner_id");
            HasManyToMany(x => x.Activities).Cascade.SaveUpdate();
        }
    }
}

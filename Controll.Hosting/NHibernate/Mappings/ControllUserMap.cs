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
    internal class ControllUserMap : SubclassMap<ControllUser>
    {
        internal ControllUserMap()
        {
            Map(x => x.Email).Unique().Column("Email");
            Map(x => x.Password).Not.Nullable().Column("Password"); ;
            Map(x => x.UserName).Unique().Not.Nullable().Column("Username");

            HasMany(x => x.LogBooks).Cascade.All();
            HasMany(x => x.Zombies).Cascade.All().KeyColumn("Owner_id");
        }
    }
}

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
    public class ControllUserMap : SubclassMap<ControllUser>
    {
        public ControllUserMap()
        {
            Map(x => x.EMail).Unique();
            Map(x => x.Password);
            Map(x => x.UserName).Unique();

            HasMany(x => x.Zombies).Cascade.All().Not.LazyLoad();
        }
    }
}

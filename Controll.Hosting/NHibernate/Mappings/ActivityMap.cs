using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    public class ActivityMap : ClassMap<Activity>
    {
        public ActivityMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Name).Column("Name");
            Map(x => x.CreatorName).Column("CreatorName");
            Map(x => x.LastUpdated).Column("LastUpdated");
            Map(x => x.Description).Column("Description");
            Map(x => x.Version).Column("Version");
            HasMany(x => x.Commands)
                .Cascade.AllDeleteOrphan()
                .Not.LazyLoad();
        }
    }
}

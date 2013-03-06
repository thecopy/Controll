using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using FluentNHibernate.Mapping;

namespace Controll.NHibernate.Mappings
{
    public class ActivityViewModelMap : ClassMap<ActivityViewModel>
    {
        public ActivityViewModelMap()
        {
            Id(x => x.Key).Column("Id").GeneratedBy.Assigned();
            Map(x => x.CreatorName);
            Map(x => x.Description);
            Map(x => x.LastUpdated);
            Map(x => x.Name);
            Map(x => x.Price);
            Map(x => x.Version);
        }
    }
}

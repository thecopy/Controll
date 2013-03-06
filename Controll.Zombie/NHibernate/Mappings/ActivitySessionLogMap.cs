using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;

namespace Controll.NHibernate.Mappings
{
    public class ActivitySessionLogMap : ClassMap<ActivitySessionLog>
    {
        public ActivitySessionLogMap()
        {
            Id(x => x.Ticket);
            References(x => x.Activity);
            Map(x => x.Completed);
            Map(x => x.Started);
            HasMany(x => x.Parameters)
                .AsMap<string>(index => index.Column("ParameterName").Type<string>(),
                               element => element.Column("ParameterValue").Type<string>())
                .Cascade.All(); 
        }
    }
}

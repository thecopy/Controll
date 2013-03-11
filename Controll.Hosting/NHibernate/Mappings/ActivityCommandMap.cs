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
    public class ActivityCommandMap : ClassMap<ActivityCommand>
    {
        public ActivityCommandMap()
        {
            Id(x => x.Id);
            Map(x => x.IsQuickCommand);
            Map(x => x.Label);
            Map(x => x.Name);
            HasMany(x => x.ParameterDescriptors).Cascade.All().Not.LazyLoad();
        }
    }
}

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
            // TODO: GeneratedBy skall inte vara assigned såklart...
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Name);
            Map(x => x.CreatorName);
            Map(x => x.LastUpdated);
            Map(x => x.Description);
            Map(x => x.Version);
            Map(x => x.FilePath);
            HasMany(x => x.Commands).Cascade.All().Not.LazyLoad();
        }
    }
}

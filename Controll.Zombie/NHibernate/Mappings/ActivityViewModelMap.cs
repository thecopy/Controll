using Controll.Common;
using FluentNHibernate.Mapping;

namespace Controll.Zombie.NHibernate.Mappings
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
            Map(x => x.Version);
        }
    }
}

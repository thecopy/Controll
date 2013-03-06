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
    public class ControllUserMap : ClassMap<ControllUser>
    {
        public ControllUserMap()
        {
            Id(x => x.Id);
            Map(x => x.EMail).Unique();
            Map(x => x.Password);
            Map(x => x.UserName).Unique();
            HasMany(x => x.ConnectedClients).Component(
                c =>
                    {
                        c.Map(x => x.ConnectionId);
                        c.Map(x => x.DeviceType);
                    }).Not.LazyLoad().Cascade.All();

            HasMany(x => x.Zombies).Cascade.All().Not.LazyLoad();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    internal class ActivityInvocationQueueItemMap : SubclassMap<ActivityInvocationQueueItem>
    {
        internal ActivityInvocationQueueItemMap()
        {
            References(x => x.Activity);
            Map(x => x.CommandName);
            Map(x => x.Responded);
            Map(x => x.Response);

            HasMany(x => x.Parameters)
                .AsMap<string>(index => index.Column("InvokedParameterName").Type<string>(),
                               element => element.Column("InvokedParameterValue").Type<string>())
                .Table("InvocationParameters")
                .Cascade.All();

            HasMany(x => x.MessageLog)
                .Cascade.AllDeleteOrphan()
                .KeyColumn("Id")
                .Component(c =>
                    {
                        c.Map(x => x.Date);
                        c.Map(x => x.Message);
                        c.Map(x => x.Type);
                    }
                );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    public class LogBookMapping : ClassMap<LogBook>
    {
        public LogBookMapping()
        {
            Id(x => x.Id);

            HasMany(x => x.LogMessages).Cascade.All();
            Map(x => x.InvocationTicket);
            References(x => x.Activity);
        }
    }
}

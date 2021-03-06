﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    public class LogMessageMapping : ClassMap<LogMessage>
    {
        public LogMessageMapping()
        {
            Id(x => x.Id);

            Map(x => x.Date);
            Map(x => x.Message);
            Map(x => x.Type);
        }
    }
}

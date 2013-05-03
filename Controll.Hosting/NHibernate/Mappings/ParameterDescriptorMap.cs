﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.Models
{
    public class ParameterDescriptorMap : ClassMap<ParameterDescriptor>
    {
        public ParameterDescriptorMap()
        {
            Id(x => x.Id);
            Map(x => x.Description);
            Map(x => x.Label);
            Map(x => x.Name);
            Map(x => x.IsBoolean);
            HasMany(x => x.PickerValues).Cascade.All();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.Models
{
    public class ParameterDescriberMap : ClassMap<ParameterDescriptor>
    {
        public ParameterDescriberMap()
        {
            Id(x => x.Id);
            Map(x => x.Description);
            Map(x => x.Label);
            Map(x => x.Name);
            HasMany(x => x.PickerValues).Element("Value");

        }
    }
}

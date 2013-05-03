using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FluentNHibernate.Mapping;

namespace Controll.Hosting.NHibernate.Mappings
{
    internal class PickerValueMap : ClassMap<PickerValue>
    {
        public PickerValueMap()
        {
            Id(x => x.Id).GeneratedBy.Guid();
            Map(x => x.Description);
            Map(x => x.Label);
            Map(x => x.Identifier);

            Map(x => x.IsCommand);
            Map(x => x.CommandName);
            HasMany(x => x.Parameters)
                .AsMap<string>(index => index.Column("ParameterName").Type<string>(),
                               element => element.Column("ParameterValue").Type<string>())
                .Table("PickerValueCommandParameters")
                .Cascade.All();
        }
    }
}

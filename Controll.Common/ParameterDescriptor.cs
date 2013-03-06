using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Common
{
    public class ParameterDescriptor
    {
        public virtual string Label { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual IList<string> PickerValues { get; set; }
        public virtual long Id { get; set; }
    }
}

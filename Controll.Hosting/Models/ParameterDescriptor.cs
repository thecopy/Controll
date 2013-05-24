using System.Collections.Generic;

namespace Controll.Hosting.Models
{
    public class ParameterDescriptor
    {
        public virtual string Label { get; set; }
        public virtual string Name { get; set; }
        public virtual bool IsBoolean { get; set; }
        public virtual string Description { get; set; }
        public virtual IList<PickerValue> PickerValues { get; set; }
        public virtual long Id { get; set; }

        public ParameterDescriptor()
        {
            PickerValues = new List<PickerValue>();
        }
    }
}

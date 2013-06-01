using System.Collections.Generic;

namespace Controll.Hosting.Models
{
    public class ParameterDescriptor
    {
        private IList<PickerValue> _pickerValues = new List<PickerValue>();

        public virtual string Label { get; set; }
        public virtual string Name { get; set; }
        public virtual bool IsBoolean { get; set; }
        public virtual string Description { get; set; }
        public virtual IList<PickerValue> PickerValues
        {
            get { return _pickerValues; }
            set { _pickerValues = value; }
        }

        public virtual long Id { get; set; }
    }
}

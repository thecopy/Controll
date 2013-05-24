using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Common.ViewModels
{
    public class ParameterDescriptorViewModel
    {
        public IEnumerable<PickerValueViewModel> PickerValues { get; set; }
        public string Label { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsBoolean { get; set; }

        public ParameterDescriptorViewModel()
        {
            PickerValues = new List<PickerValueViewModel>();
        }
    }
}

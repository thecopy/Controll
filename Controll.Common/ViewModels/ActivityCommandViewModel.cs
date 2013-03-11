using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Common.ViewModels
{
    public class ActivityCommandViewModel
    {
        public IEnumerable<ParameterDescriptorViewModel> ParameterDescriptors { get; set; }
        public bool IsQuickCommand { get; set; }
        public string Label { get; set; }
        public string Name { get; set; }
    }
}

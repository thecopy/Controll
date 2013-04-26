using System.Collections.Generic;
using Controll.Common;

namespace Controll.Hosting.Models
{
    public class ActivityCommand
    {
        public virtual long Id { get; set; }
        public virtual string Label { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<ParameterDescriptor> ParameterDescriptors { get; set; } 

        public ActivityCommand()
        {
            ParameterDescriptors = new List<ParameterDescriptor>();
        }
    }
}

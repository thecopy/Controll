using System;
using System.Collections.Generic;

namespace Controll.Hosting.Models
{
    public class ActivityCommand
    {
        public virtual Guid Id { get; set; }
        public virtual string Label { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<ParameterDescriptor> ParameterDescriptors { get; set; } 
    }
}

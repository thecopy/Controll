using System;
using System.Collections.Generic;

namespace Controll.Hosting.Models
{
    public class ActivityCommand
    {
        private IList<ParameterDescriptor> _parameterDescriptors = new List<ParameterDescriptor>();
        public virtual Guid Id { get; set; }
        public virtual string Label { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<ParameterDescriptor> ParameterDescriptors  
        {
            get { return _parameterDescriptors; }
            set { _parameterDescriptors = value; }
        }
    }
}

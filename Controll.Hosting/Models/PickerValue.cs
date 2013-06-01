using System;
using System.Collections.Generic;

namespace Controll.Hosting.Models
{
    public class PickerValue
    {
        private IDictionary<string, string> _parameters = new Dictionary<string, string>();
        public virtual Guid Id { get; set; } // Database-Id
        
        public virtual string Label { get; set; }
        public virtual string Description { get; set; }
        public virtual string Identifier { get; set; } // Used by the activity

        public virtual bool IsCommand { get; set; } 
        public virtual string CommandName { get; set; }
        public virtual IDictionary<string, string> Parameters       
        {
            get { return _parameters; }
            set { _parameters = value; }
        }
    }
}
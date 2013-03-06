using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActivityAttribute : Attribute
    {
        public Guid Key { get; set; }

        public ActivityAttribute(string key) : base()
        {
            Key = new Guid(key);
        }
    }
}

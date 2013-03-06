using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;

namespace Controll.Hosting.Models
{
    public class Device
    {
        public virtual Guid Id { get; set; }
        public virtual DeviceType Type { get; set; }
        public virtual string FriendyName { get; set; }
        public virtual string Name { get; set; }
        public virtual string CurrentConnectionId { get; set; }
    }
}

using System.Collections.Generic;
using Controll.Common.ViewModels;

namespace Controll.Hosting.Models
{
    public class Zombie
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string ConnectionId { get; set; }
        public virtual IList<Activity> Activities { get; set; } 
    }
}

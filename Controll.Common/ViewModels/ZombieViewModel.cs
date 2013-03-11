using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll
{
    public class ZombieViewModel
    {
        public ZombieViewModel(){} 

        public ZombieViewModel(IEnumerable<Activity> activities, string name, bool isOnline)
        {
            IsOnline = isOnline;
            Name = name;
            Activities = activities;
        }

        public string Name { get; set; }
        public IEnumerable<Activity> Activities { get; set; }
        public bool IsOnline { get; set; }
    }
}

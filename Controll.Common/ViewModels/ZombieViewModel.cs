using System.Collections.Generic;

namespace Controll.Common.ViewModels
{
    public class ZombieViewModel
    {
        public string Name { get; set; }
        public IEnumerable<ActivityViewModel> Activities { get; set; }
        public bool IsOnline { get; set; }
    }
}

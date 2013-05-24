using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Common.ViewModels
{
    public class ActivityIntermidiateCommandViewModel : ActivityCommandViewModel
    {
        public ActivityViewModel Activity { get; set; }

        /// <summary>
        /// The ticket for the ActivityResultQueueItem which is being represented
        /// </summary>
        public Guid ResultTicket { get; set; }

        public ActivityIntermidiateCommandViewModel()
        {}

        public ActivityIntermidiateCommandViewModel(
            ActivityCommandViewModel commandViewModel, 
            ActivityViewModel activityViewModel,
            Guid resultTicket)
        {
            Label = commandViewModel.Label;
            Name = commandViewModel.Name;
            ParameterDescriptors = commandViewModel.ParameterDescriptors;

            ResultTicket = resultTicket;
            Activity = activityViewModel;
        }
    }
}

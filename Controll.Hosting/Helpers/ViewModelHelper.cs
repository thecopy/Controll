using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;

namespace Controll.Hosting.Helpers
{
    public static class ViewModelHelper
    {
        public static ActivityCommandViewModel Create(ActivityCommand command)
        {
            return new ActivityCommandViewModel().Fill(command);
        }

        public static ActivityCommandViewModel Fill(this ActivityCommandViewModel self, ActivityCommand command)
        {
            self.Name = command.Name;
            self.Label = command.Label;
            self.IsQuickCommand = command.IsQuickCommand;
            self.ParameterDescriptors =
                command.ParameterDescriptors.Select(pd => new ParameterDescriptorViewModel(pd)).ToList();

            return self;
        }

        public static ActivityViewModel Fill(this ActivityViewModel self, Activity activity)
        {
            self.Key = activity.Id;
            self.Name = activity.Name;
            self.CreatorName = activity.CreatorName;
            self.Version = activity.Version;
            self.Description = activity.Description;
            self.LastUpdated = activity.LastUpdated;
            self.Commands = activity.Commands.Select(c => ViewModelExtensions.Create(c));

            return self;
        }
    }
}

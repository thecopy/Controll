using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Models;

namespace Controll.Hosting.Helpers
{
    public static class ViewModelHelper
    {
        public static ZombieViewModel Fill(this ZombieViewModel self, Zombie zombie)
        {
            self.Activities = zombie.Activities.Select(CreateViewModel);
            self.Name = zombie.Name;
            self.IsOnline = zombie.IsOnline();

            return self;
        }

        public static ParameterDescriptorViewModel Fill(this ParameterDescriptorViewModel self,
                                                        ParameterDescriptor descriptor)
        {
            self.Description = descriptor.Description;
            self.Name = descriptor.Name;
            self.Label = descriptor.Label;
            self.PickerValues = descriptor.PickerValues;

            return self;
        }

        public static ActivityCommandViewModel Fill(this ActivityCommandViewModel self, ActivityCommand command)
        {
            self.Name = command.Name;
            self.Label = command.Label;
            self.IsQuickCommand = command.IsQuickCommand;
            self.ParameterDescriptors = command.ParameterDescriptors.Select(CreateViewModel);

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
            self.Commands = activity.Commands.Select(CreateViewModel);

            return self;
        }

        public static ActivityCommandViewModel CreateViewModel(ActivityCommand command)
        {
            return new ActivityCommandViewModel().Fill(command);
        }

        public static ActivityViewModel CreateViewModel(Activity activity)
        {
            return new ActivityViewModel().Fill(activity);
        }

        public static ZombieViewModel CreateViewModel(Zombie zombie)
        {
            return new ZombieViewModel().Fill(zombie);
        }

        public static ParameterDescriptorViewModel CreateViewModel(ParameterDescriptor parameterDescriptor)
        {
            return new ParameterDescriptorViewModel().Fill(parameterDescriptor);
        }
    }
}

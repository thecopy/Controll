using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Models;
using ParameterDescriptor = Controll.Hosting.Models.ParameterDescriptor;

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
            self.IsBoolean = descriptor.IsBoolean;
            self.PickerValues = descriptor.PickerValues == null
                                    ? new List<PickerValueViewModel>()
                                    : descriptor.PickerValues.Select(pvvm => new PickerValueViewModel()
                                        {
                                            IsCommand = pvvm.IsCommand,
                                            Label = pvvm.Label,
                                            Description = pvvm.Description,
                                            Identifier = pvvm.Identifier,
                                            CommandName = pvvm.CommandName,
                                            Parameters = pvvm.Parameters
                                        }).ToList();

            return self;
        }

        public static ActivityCommandViewModel Fill(this ActivityCommandViewModel self, ActivityCommand command)
        {
            self.Name = command.Name;
            self.Label = command.Label;
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


        public static ActivityCommandViewModel CreateViewModel(this ActivityCommand command)
        {
            return new ActivityCommandViewModel().Fill(command);
        }

        public static ActivityViewModel CreateViewModel(this Activity activity)
        {
            return new ActivityViewModel().Fill(activity);
        }

        public static ZombieViewModel CreateViewModel(this Zombie zombie)
        {
            return new ZombieViewModel().Fill(zombie);
        }

        public static ParameterDescriptorViewModel CreateViewModel(this ParameterDescriptor parameterDescriptor)
        {
            return new ParameterDescriptorViewModel().Fill(parameterDescriptor);
        }

        public static Activity CreateConcreteClass(this ActivityViewModel activityViewModel)
        {
            return new Activity
                {
                    Id = activityViewModel.Key,
                    Name = activityViewModel.Name,
                    LastUpdated = activityViewModel.LastUpdated,
                    CreatorName = activityViewModel.CreatorName,
                    Description = activityViewModel.Description,
                    Version = activityViewModel.Version,
                    Commands = activityViewModel.Commands.Select(c => new ActivityCommand
                        {
                            Label = c.Label,
                            Name = c.Name,
                            ParameterDescriptors = c.ParameterDescriptors.Select(p => new ParameterDescriptor
                                {
                                    Description = p.Description,
                                    Label = p.Label,
                                    Name = p.Name,
                                    IsBoolean = p.IsBoolean,
                                    PickerValues = p.PickerValues == null
                                                       ? new List<PickerValue>()
                                                       : p.PickerValues.Select(pvvm => new PickerValue
                                                           {
                                                               IsCommand = pvvm.IsCommand,
                                                               Label = pvvm.Label,
                                                               Description = pvvm.Description,
                                                               CommandName = pvvm.CommandName,
                                                               Parameters = pvvm.Parameters
                                                           }).ToList()
                                }).ToList()
                        }).ToList()
                };
        }
    }
}

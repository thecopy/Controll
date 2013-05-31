using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;

namespace SimpleSamplePlugin
{
    [ActivityAttribute("3F7DB79F-A596-4A1A-852F-1B0EF287D479")]
    public class SampleActivity : IActivity
    {
        public void Execute(IActivityContext context)
        {
            var command = context.CommandName;
            if (command == "do-nothing")
            {
                string callerName = context.Parameters["name"];
                context.Message(ActivityMessageType.Notification, "Hello " + callerName);
            }
            else if(command == "intermidiate-command")
            {
                var commandViewModel = new ActivityCommandViewModel
                    {
                        Label = "Intermidiate Command:)",
                        Name = "intermidiate-command-result",
                        ParameterDescriptors = new List<ParameterDescriptorViewModel>
                            {
                                new ParameterDescriptorViewModel
                                    {
                                        Description = "Just some text",
                                        Label = "Text",
                                        Name = "text"
                                    },
                                new ParameterDescriptorViewModel
                                    {
                                        Description = "Pick a value",
                                        Label = "Values",
                                        Name = "picked-value",
                                        PickerValues = new List<PickerValueViewModel>
                                            {
                                                new PickerValueViewModel
                                                    {
                                                        Label = "Apple",
                                                        Identifier = "apple"
                                                    },
                                                new PickerValueViewModel
                                                    {
                                                        Label = "Banana",
                                                        Identifier = "banana"
                                                    },
                                                new PickerValueViewModel
                                                    {
                                                        Label = "Pear",
                                                        Identifier = "pear"
                                                    }
                                            }
                                    }
                            }
                    };

                context.Result(commandViewModel);
            }else if (command == "intermidiate-command-result")
            {
                context.Message(ActivityMessageType.Notification, "You entered the text '" + context.Parameters["text"] + "'");
                context.Message(ActivityMessageType.Notification, "You chose the value '" + context.Parameters["picked-value"] + "'");
            }
        }

        public ActivityViewModel ViewModel { get { return _viewModel; } }
        private readonly ActivityViewModel _viewModel = new ActivityViewModel
            {
                Key = Guid.Parse("3F7DB79F-A596-4A1A-852F-1B0EF287D479"),
                Name = "Sample Activity",
                Description = "An activity which does nothing",
                CreatorName = "thecopy",
                LastUpdated = DateTime.Parse("2013-04-25"),
                Version = new Version(1,0,0,0),

                Commands = new List<ActivityCommandViewModel>
                    {
                        new ActivityCommandViewModel
                            {
                                Label = "Do Nothing",
                                Name = "do-nothing",
                                ParameterDescriptors = new List<ParameterDescriptorViewModel>
                                    {
                                        new ParameterDescriptorViewModel
                                            {
                                                Label = "Your Name",
                                                Description = "Please enter your name",
                                                Name = "name"
                                            }
                                    }
                            },
                            new ActivityCommandViewModel
                                {
                                    Label = "Create an Intermidiate Command",
                                    Name = "intermidiate-command"
                                }
                    }
            };
    }
}

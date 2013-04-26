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
    [ActivityAttribute("1925C00C-7BD8-4D5D-BD34-78CD1D7D0EA3")]
    public class SampleActivity : IActivity
    {
        public void Execute(IActivityContext context)
        {
            context.Started();

            var command = context.Parameters["__command"];
            if (command == "do-nothing")
            {
                string callerName = context.Parameters["name"];
                context.Finish("Hello " + callerName);
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
                                        PickerValues = new List<string> {"apple", "pear", "banana"}
                                    }
                            }
                    };

                context.Result(commandViewModel);
                context.Finish("OK");
            }else if (command == "intermidiate-command-result")
            {
                context.Notify("You entered the text '" + context.Parameters["text"] + "'");
                context.Notify("You chose the value '" + context.Parameters["picked-value"] + "'");
                context.Finish("OK");
            }
        }

        public ActivityViewModel ViewModel { get { return _viewModel; } }
        private readonly ActivityViewModel _viewModel = new ActivityViewModel
            {
                Key = Guid.Parse("1925C00C-7BD8-4D5D-BD34-78CD1D7D0EA3"),
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

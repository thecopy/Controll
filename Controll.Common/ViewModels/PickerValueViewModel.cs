using System.Collections.Generic;

namespace Controll.Common.ViewModels
{
    public class PickerValueViewModel
    {
        public string Label { get; set; }
        public string Description { get; set; }
        public string Identifier { get; set; }

        public bool IsCommand { get; set; }
        public string CommandName { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}
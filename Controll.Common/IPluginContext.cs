using System.Collections.Generic;

namespace Controll.Common
{
    public interface IPluginContext
    {
        IDictionary<string, string> Parameters { get; }
        object[] Arguments { get; }
        void Started();
        void Finish(string result);
        void Error(string errorMessage);
        void Notify(string message);
    }
}

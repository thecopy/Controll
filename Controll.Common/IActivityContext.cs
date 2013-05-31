using System.Collections.Generic;

namespace Controll.Common
{
    public interface IActivityContext
    {
        IDictionary<string, string> Parameters { get; }
        string CommandName { get; }
        void Notify(string message);
        void Result(object result);
    }
}

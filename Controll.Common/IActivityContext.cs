using System.Collections.Generic;

namespace Controll.Common
{
    public interface IActivityContext
    {
        IDictionary<string, string> Parameters { get; }
        string CommandName { get; }
        void Message(ActivityMessageType type, string message);
        void Result(object result);
    }
}

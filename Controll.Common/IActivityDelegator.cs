using System;
using System.Threading.Tasks;

namespace Controll.Common
{
    public interface IActivityDelegator
    {
        void ActivityMessage(Guid ticket, ActivityMessageType type, string message);
        void ActivityResult(Guid ticket, object result);
    }
}

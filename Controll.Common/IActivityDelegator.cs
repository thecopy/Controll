using System;
using System.Threading.Tasks;

namespace Controll.Common
{
    public interface IActivityDelegator
    {
        Task ActivityMessage(Guid ticket, ActivityMessageType type, string message = null);
        Task ActivityResult(Guid ticket, object result);
    }
}

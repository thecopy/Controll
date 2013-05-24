using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll.Common.Authentication
{
    /// <summary>
    /// Interface that wraps SignalR's IClientTransport and provides a way to add authentication information
    /// </summary>
    public interface IAuthenticationProvider
    {
        Task<HubConnection> Connect(string userName, string password, string zombie);
    }
}

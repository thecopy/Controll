using Controll.Hosting.Models;

namespace Controll.Hosting.Services
{
    public interface IMembershipService
    {
        ControllUser AuthenticateUser(string userName, string password);
        ControllUser AddUser(string userName, string password, string email);
    }
}

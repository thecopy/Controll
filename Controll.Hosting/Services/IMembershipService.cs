using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;

namespace Controll.Hosting.Services
{
    public interface IMembershipService
    {
        ControllUser AuthenticateUser(string userName, string password);
        ControllUser AddUser(string userName, string password, string email);
    }
}

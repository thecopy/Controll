using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;

namespace Controll.Hosting.Tests
{
    internal class InMemoryControllUserRepository : InMemoryRepository<ControllUser>, IControllUserRepository
    {
        public ControllUser GetByUserName(string userName)
        {
            return Collection.SingleOrDefault(e => e.UserName == userName);
        }

        public ControllUser GetByConnectionId(string connectionId)
        {
            return Collection.SingleOrDefault(e => e.ConnectedClients.Count(c => c.ConnectionId == connectionId) > 0);
        }

        public ControllUser GetByEMail(string email)
        {
            return Collection.SingleOrDefault(e => e.EMail == email);
        }
    }
}

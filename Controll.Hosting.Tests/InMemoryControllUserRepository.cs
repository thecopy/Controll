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
    internal class InMemoryControllUserRepository : IControllUserRepository
    {
        private readonly ICollection<ControllUser> _collection;

        public InMemoryControllUserRepository()
        {
            _collection = new Collection<ControllUser>();
        }

        public void Add(ControllUser entity)
        {
            _collection.Add(entity);
        }

        public void Update(ControllUser entity)
        {
            //
        }

        public ControllUser Get(object identifier)
        {
            return _collection.SingleOrDefault(e => e.Id == (int)identifier);
        }

        public void Remove(ControllUser entity)
        {
            _collection.Remove(entity);
        }

        public IList<ControllUser> GetAll(int maxResults = 100)
        {
            return _collection.Take(maxResults).ToList();
        }

        public ControllUser GetByUserName(string userName)
        {
            return _collection.SingleOrDefault(e => e.UserName == userName);
        }

        public ControllUser GetByConnectionId(string connectionId)
        {
            return _collection.SingleOrDefault(e => e.ConnectedClients.Count(c => c.ConnectionId == connectionId) > 0);
        }

        public ControllUser GetByEMail(string email)
        {
            return _collection.SingleOrDefault(e => e.EMail == email);
        }
    }
}

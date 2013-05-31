using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using Controll.Hosting.Models.Queue;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace Controll.Hosting.Repositories
{
    public class ControllRepository : IControllRepository
    {
        private readonly ISession _session;

        public ControllRepository(ISession session)
        {
            _session = session;
        }

        public ControllClient GetClientByConnectionId(string connectionId)
        {
            return _session.QueryOver<ControllClient>()
                                 .Where(x => x.ConnectionId == connectionId)
                                 .SingleOrDefault();
        }

        public T GetClientByConnectionId<T>(string connectionId) where T : ClientCommunicator
        {
            var client = GetClientByConnectionId(connectionId);

            if (client == null)
                return null;

            return (T)client.ClientCommunicator;
        }


        public ControllUser GetUserFromUserName(string username)
        {
            return _session.QueryOver<ControllUser>()
                           .Where(x => x.UserName == username)
                           .SingleOrDefault();
        }

        public ControllUser GetUserFromEmail(string email)
        {
            return _session.QueryOver<ControllUser>()
                           .Where(x => x.Email == email)
                           .SingleOrDefault();
        }

        public IList<QueueItem> GetUndeliveredQueueItemsForZombie(int zombieId, int take = 100, int skip = 0)
        {
            if(take > 100)
                throw new InvalidOperationException("Cannot take more than 100 queue items. Specify a number of items to skip instead.");

            return _session.QueryOver<QueueItem>()
                          .Where(qi => qi.Reciever.Id == zombieId && !qi.Delivered.HasValue)
                          .Skip(skip)
                          .Take(take)
                          .List();
        }
    }
}

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
            var client = _session.CreateCriteria<ControllClient>()
                    .Add(Restrictions.Eq("ConnectionId", connectionId))
                    .SetMaxResults(1)
                    .UniqueResult<ControllClient>();

            return client;
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
            return _session.CreateCriteria<ControllUser>()
                                 .Add(Restrictions.Eq("UserName", username))
                                 .SetMaxResults(1)
                                 .UniqueResult<ControllUser>();
        }

        public ControllUser GetUserFromEmail(string email)
        {
            return _session.CreateCriteria<ControllUser>()
                     .Add(Restrictions.Eq("Email", email))
                     .SetMaxResults(1)
                     .UniqueResult<ControllUser>();
        }

        public IList<QueueItem> GetUndeliveredQueueItemsForZombie(int zombieId)
        {
            return _session.Query<QueueItem>()
                          .Where(qi => qi.Reciever.Id == zombieId && !qi.Delivered.HasValue)
                          .Take(100)
                          .ToList();
        }
    }
}

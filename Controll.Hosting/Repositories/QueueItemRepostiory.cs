using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models.Queue;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace Controll.Hosting.Repositories
{
    public interface IQueueItemRepostiory : IGenericRepository<QueueItem>
    {
        IList<QueueItem> GetUndeliveredQueueItemsForZombie(int zombieId);
    }

    public class QueueItemRepostiory : GenericRepository<QueueItem>, IQueueItemRepostiory
    {
        public QueueItemRepostiory(ISession session) : base(session)
        {
        }


        public IList<QueueItem> GetUndeliveredQueueItemsForZombie(int zombieId)
        {
            return Session.Query<QueueItem>()
                .Where(qi => qi.Reciever.Id == zombieId && !qi.Delivered.HasValue)
                .ToList();

        }
    }
}

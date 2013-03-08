using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
using NHibernate;
using NHibernate.Criterion;

namespace Controll.Hosting.Repositories
{
    public interface IControllUserRepository : IGenericRepository<ControllUser>
    {
        ControllUser GetByUserName(string userName);
        ControllUser GetByConnectionId(string connectionId);
        ControllUser GetByEMail(string email);
    }

    public class ControllUserRepository : GenericRepository<ControllUser>, IControllUserRepository
    {
        public ControllUserRepository(ISession session) : base(session)
        {}

        public ControllUser GetByUserName(string userName)
        {
                return Session.CreateCriteria<ControllUser>()
                    .Add(Restrictions.Eq("UserName", userName))
                    .UniqueResult<ControllUser>();            
        }

        public ControllUser GetByConnectionId(string connectionId)
        {
                return Session.CreateCriteria<ControllUser>()
                    .CreateCriteria("ConnectedClients", "clients")
                    .Add(Restrictions.Eq("clients.ConnectionId", connectionId))
                    .UniqueResult<ControllUser>();     
        }

        public ControllUser GetByEMail(string email)
        {
                return Session.CreateCriteria<ControllUser>()
                    .Add(Restrictions.Eq("EMail", email))
                    .UniqueResult<ControllUser>();      
        }
    }
}

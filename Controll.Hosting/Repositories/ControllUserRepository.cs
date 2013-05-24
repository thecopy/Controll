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
                    .SetMaxResults(1)
                    .UniqueResult<ControllUser>();            
        }

        public ControllUser GetByConnectionId(string connectionId)
        {
            var user = Session.CreateCriteria<ControllClient>()
                    .Add(Restrictions.Eq("ConnectionId", connectionId))
                    .SetMaxResults(1)
                    .UniqueResult<ControllClient>();

            if (user == null)
                return null;

            return user.ClientCommunicator as ControllUser;
        }

        public ControllUser GetByEMail(string email)
        {
                return Session.CreateCriteria<ControllUser>()
                    .Add(Restrictions.Eq("Email", email))
                    .SetMaxResults(1)
                    .UniqueResult<ControllUser>();      
        }
    }
}

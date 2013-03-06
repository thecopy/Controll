using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using Controll.Hosting.NHibernate;
<<<<<<< HEAD
using NHibernate;
=======
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
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
<<<<<<< HEAD
        public ControllUserRepository(ISession session) : base(session)
        {}

        public ControllUser GetByUserName(string userName)
        {
            using (var session = Session)
=======
        public ControllUser GetByUserName(string userName)
        {
            using (var session = NHibernateHelper.OpenSession())
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
                return session.CreateCriteria<ControllUser>()
                    .Add(Restrictions.Eq("UserName", userName))
                    .UniqueResult<ControllUser>();            
        }

        public ControllUser GetByConnectionId(string connectionId)
        {
<<<<<<< HEAD
            using (var session = Session)
=======
            using (var session = NHibernateHelper.OpenSession())
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
                return session.CreateCriteria<ControllUser>()
                    .CreateCriteria("ConnectedClients", "clients")
                    .Add(Restrictions.Eq("clients.ConnectionId", connectionId))
                    .UniqueResult<ControllUser>();     
        }

        public ControllUser GetByEMail(string email)
        {
<<<<<<< HEAD
            using (var session = Session)
=======
            using (var session = NHibernateHelper.OpenSession())
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
                return session.CreateCriteria<ControllUser>()
                    .Add(Restrictions.Eq("EMail", email))
                    .UniqueResult<ControllUser>();      
        }
    }
}

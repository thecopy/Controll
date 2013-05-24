using System;
using System.Linq;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using NHibernate;
using NHibernate.Criterion;

namespace Controll.Hosting.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly ISession _session;
        private readonly IControllRepository _controllRepository;

        public MembershipService(ISession session, IControllRepository controllRepository)
        {
            _session = session;
            _controllRepository = controllRepository;
        }

        public ControllUser AuthenticateUser(string userName, string password)
        {
            var user = GetUser(userName);

            if (user.Password != password)
                throw new InvalidOperationException(String.Format("Wrong password."));

            return user;
        }

        public ControllUser AddUser(string userName, string password, string email)
        {
            EnsureUserDontExist(userName);
            EnsureMailDontExist(email);

            var user = new ControllUser
                {
                    UserName = userName,
                    Email = email,
                    Password = password
                };

            _session.Save(user);
            return user;
        }
        
        private ControllUser GetUser(string userName)
        {
            var user = _controllRepository.GetUserFromUserName(userName);
            if (user == null)
                throw new InvalidOperationException(String.Format("Unable to find user {0}.", userName));
            return user;
        }

        private void EnsureUserDontExist(string userName)
        {
            var rowcount = (int) _session.CreateCriteria<ControllUser>()
                                         .Add(Restrictions.Eq("UserName", userName))
                                         .SetProjection(Projections.RowCountInt64())
                                         .UniqueResult();
            if (rowcount > 0)
                ThrowUserExists(userName);
        }

        private void EnsureMailDontExist(string email)
        {
            var rowcount = (int)_session.CreateCriteria<ControllUser>()
                                         .Add(Restrictions.Eq("Email", email))
                                         .SetProjection(Projections.RowCountInt64())
                                         .UniqueResult();
            if (rowcount > 0)
                ThrowUserExists(email);
        }

        internal static void ThrowUserExists(string userName)
        {
            throw new InvalidOperationException(String.Format("Username {0} already taken.", userName));
        }

        internal static void ThrowMailExists(string email)
        {
            throw new InvalidOperationException(String.Format("Email {0} already taken.", email));
        }
    }
}
using System;
using System.Linq;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;

namespace Controll.Hosting.Services
{
    internal class MembershipService : IMembershipService
    {
        private readonly ControllUserRepository _userRepository;

        public MembershipService(ControllUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public ControllUser AuthenticateUser(string userName, string password)
        {
            var user = GetUser(userName);

            if (user.Password != password)
                return null;

            return user;
        }

        public ControllUser AddUser(string userName, string password, string email)
        {
            EnsureUserDontExist(userName);

            var user = new ControllUser
                {
                    UserName = userName,
                    Email = email,
                    Password = password
                };

            _userRepository.Add(user);
            return user;
        }
        
        private ControllUser GetUser(string userName)
        {
            var user = _userRepository.GetByUserName(userName);
            if (user == null)
                throw new InvalidOperationException(String.Format("Unable to find user {0}.", userName));
            return user;
        }

        private void EnsureUserDontExist(string userName)
        {
            if(_userRepository.Query.Any(user => user.UserName == userName))
                ThrowUserExists(userName);
        }

        internal static void ThrowUserExists(string userName)
        {
            throw new InvalidOperationException(String.Format("Username {0} already taken.", userName));
        }
    }
}
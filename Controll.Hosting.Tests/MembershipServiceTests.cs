using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Moq;
using NHibernate;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    public class MembershipServiceTests
    {
        [Test]
        public void ShouldBeAbleToRegisterUser()
        {
            var mockedSession = new Mock<ISession>();
            var mockedRepository = new Mock<IControllRepository>();
            var service = new MembershipService(mockedSession.Object, mockedRepository.Object);

            mockedRepository.Setup(x => x.GetUserFromEmail(It.IsAny<String>())).Returns((ControllUser) null);
            mockedRepository.Setup(x => x.GetUserFromUserName(It.IsAny<String>())).Returns((ControllUser)null);

            var user = service.AddUser("username", "password", "email");

            Assert.AreEqual("username", user.UserName);
            Assert.AreEqual("email", user.Email);
        }

        [Test]
        public void ShouldNotBeAbleToRegisterUserIfUserNameAlreadyExists()
        {
            var mockedSession = new Mock<ISession>();
            var mockedRepository = new Mock<IControllRepository>();
            var service = new MembershipService(mockedSession.Object, mockedRepository.Object);

            mockedRepository.Setup(x => x.GetUserFromEmail(It.IsAny<String>())).Returns((ControllUser)null);
            mockedRepository.Setup(x => x.GetUserFromUserName(It.IsAny<String>())).Returns(new ControllUser());

            Assert.Throws<InvalidOperationException>(() =>  service.AddUser("username", "password", "email"));
        }

        [Test]
        public void ShouldNotBeAbleToRegisterUserIfEmailAlreadyExists()
        {
            var mockedSession = new Mock<ISession>();
            var mockedRepository = new Mock<IControllRepository>();
            var service = new MembershipService(mockedSession.Object, mockedRepository.Object);

            mockedRepository.Setup(x => x.GetUserFromEmail(It.IsAny<String>())).Returns(new ControllUser());
            mockedRepository.Setup(x => x.GetUserFromUserName(It.IsAny<String>())).Returns((ControllUser)null);

            Assert.Throws<InvalidOperationException>(() => service.AddUser("username", "password", "email"));
        }
    }
}

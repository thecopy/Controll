using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Infrastructure;
using Controll.Hosting.Models;
using Controll.Hosting.Services;
using Moq;
using NHibernate;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    public class AuthenticationTests
    {
        [Test]
        public void ShouldNotBeAbleToAuthenticateIfAuthenticationFails()
        {
            var mockedService = new Mock<IMembershipService>();

            mockedService.Setup(x => x.AuthenticateUser(It.IsAny<String>(), It.IsAny<String>())).Throws<InvalidOperationException>();

            Assert.Throws<InvalidOperationException>(() => ControllAuthentication.AuthenticateForms("username", "pass", null, mockedService.Object));
        }
        [Test]
        public void ShouldNotBeAbleToAuthenticateIfZombieDoesNotExist()
        {
            var mockedService = new Mock<IMembershipService>();

            mockedService.Setup(x => x.AuthenticateUser(It.Is<String>(s => s == "username"), It.Is<String>(s => s == "pass")))
                .Returns(new ControllUser { Id = 1, Zombies = new List<Zombie>()});

            Assert.Throws<InvalidOperationException>(() => ControllAuthentication.AuthenticateForms("username", "pass", "does-not-exist", mockedService.Object));
        }

        [Test]
        public void ShouldBeAbleToAuthenticateAndReturnIdentity()
        {
            var mockedService = new Mock<IMembershipService>();

            mockedService.Setup(x => x.AuthenticateUser(It.Is<String>(s => s == "username"), It.Is<String>(s => s == "pass")))
                .Returns(new ControllUser { Id = 1 });

            var identity = ControllAuthentication.AuthenticateForms("username", "pass", null, mockedService.Object);

            Assert.True(identity.HasClaim(ControllClaimTypes.UserIdentifier, "1"));
        }

        [Test]
        public void ShouldBeAbleToAuthenticateAndReturnIdentityWithZombie()
        {
            var mockedService = new Mock<IMembershipService>();

            mockedService.Setup(x => x.AuthenticateUser(It.Is<String>(s => s == "username"), It.Is<String>(s => s == "pass")))
                         .Returns(new ControllUser {Id = 1, Zombies = new[] {new Zombie {Name = "zombie", Id = 2}}});

            var identity = ControllAuthentication.AuthenticateForms("username", "pass", "zombie", mockedService.Object);

            Assert.True(identity.HasClaim(ControllClaimTypes.UserIdentifier, "1"));
            Assert.True(identity.HasClaim(ControllClaimTypes.ZombieIdentifier, "2"));
        }
    }
}

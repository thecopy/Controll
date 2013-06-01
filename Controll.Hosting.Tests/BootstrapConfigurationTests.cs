using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Infrastructure;
using Moq;
using NHibernate;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    public class BootstrapConfigurationTests
    {
        [Test]
        public void ShouldBeAbleToDecideValidAndNonValidConfiguration()
        {
            var configuration = new ControllHostingConfiguration();

            Assert.False(configuration.IsValid); // No Connection String
            configuration.ConnectionStringAlias = "connectionString";

            Assert.True(configuration.IsValid);

            configuration.UseCustomSessionFactory = true;
            Assert.False(configuration.IsValid); // No Custom Factory

            configuration.CustomSessionFactory = new Mock<ISessionFactory>().Object;
            Assert.False(configuration.IsValid); // Connection String Alias when using Custom Factory
            
            configuration.ConnectionStringAlias = null;
            Assert.True(configuration.IsValid);

            configuration.ConnectionStringAlias = "";
            Assert.True(configuration.IsValid);

            configuration.HubScope = null;
            Assert.False(configuration.IsValid); // Null HubScope

            configuration.HubScope = "";
            Assert.False(configuration.IsValid); // Empty HubScope

        }
    }
}

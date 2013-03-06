using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Controll.Tests
{
    [TestClass]
    public class ControllClientTests
    {
        [TestInitialize]
        public void Initialize()
        {
            
        }

        // Kasst test
        public void ShouldBeAbleToConnect()
        {
            var client = new ControllClient("http://localhost:10244");
            client.Connect();
        }
    }
}

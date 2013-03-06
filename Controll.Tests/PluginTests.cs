using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Controll.Tests
{
    [TestClass]
    public class PluginTests
    {/*
        [TestMethod]
        public void ShouldRespondToServerWhenIsCompleted()
        {
            var client = new Mock<ControllClient>("http://foo/");
            client.Setup(c => c.ActivityCompleted(It.IsAny<Guid>(), It.IsAny<string>())).Verifiable();

            var clientContext = new Mock<IPluginContext>();
            clientContext.Setup(c => c.Finish(It.IsAny<string>())).Callback(
                () => client.Object.ActivityCompleted(Guid.Empty, "result"));

            var mockedPlugin = new Mock<IPlugin>();
            mockedPlugin.Setup(p => p.Execute(clientContext.Object)).Callback(()
                                                                              => clientContext.Object.Finish("result"));

            // Call the plugin to activate itself
            mockedPlugin.Object.Execute(clientContext.Object); 

            // Verify that the plugin have called upon the client to report the result to the cloud
            client.Verify(c => c.ActivityCompleted(Guid.Empty, "result"), Times.Exactly(1)); 
    }*/
    }
}

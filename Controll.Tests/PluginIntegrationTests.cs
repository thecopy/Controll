using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Controll.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SignalR.Client;
using SignalR.Client.Hubs;
using SignalR.Hosting.Memory;
using SignalR.Hubs;

namespace Controll.Tests
{

    /*
    public class FakePcHub : Hub
    {
        public Guid ActivateZombie(string name, Guid id)
        {
            Console.WriteLine("ActivateZombie @ Hub called");
            Caller.ActivateZombie(id);
            return Guid.NewGuid();
        }
    }

    [TestClass]
    public class PluginIntegrationTests
    {
        //[TestMethod]
        public void MemoryHost()
        {
            var host = new MemoryHost();
            host.MapHubs();

            var conn = new HubConnection("http://local/");
            conn.Start(host);
            conn.Stop();

        }

        //[TestMethod]
        public void ShouldGetResponseFromZombieWhenActivatingAPlugin()
        {
            var process = new Process
                {
                    StartInfo =
                        new ProcessStartInfo("..\\..\\..\\Controll.SampleServer\\bin\\Debug\\Controll.SampleServer.exe")
                            {
                                WorkingDirectory = "..\\..\\Controll.SampleServer\\bin\\Debug\\"
                            }
                };
            process.Start();

            // Wait for the server to init
            Thread.Sleep(100);

            var client = new ControllClient("http://localhost:10244");

            string zombieName = "zombie";
            Guid activityId = Guid.NewGuid();

            var plugin = GetTestablePlugin(activityId, "plugin");

            #region Events

            var waitHandler = new ManualResetEvent(false);
            var ticket = Guid.Empty;
            client.ActivateZombie += (s, e) =>
                {
                    ticket = e.ActivityTicket;
                    waitHandler.Set();
                };

            string result = null;
            var waitHandler2 = new ManualResetEvent(false);
            client.ZombieActivityCompleted += (s, e) =>
                {
                    result = (string)e.Result; 
                    waitHandler2.Set();
                };
            #endregion

            client.Connect();
            client.ActivateZombieActivity(zombieName, Guid.Empty, "p");

            Assert.IsTrue(waitHandler.WaitOne(2000), "client.ActivateZombie did not fire");

            plugin.Execute(new TestablePluginContext (client));

            Assert.IsTrue(waitHandler2.WaitOne(1000), "client.ZombieActivityCompleted did not fire");
            Assert.AreEqual("RESULT", result);

            process.Kill();
        }

        private class TestablePluginContext : IPluginContext
        {
            public TestablePluginContext(IControllPluginClient client)
            {
                Client = client;
            }

            private IControllPluginClient Client { get; set; }
            public string Parameter { get; private set; }
            public Dictionary<string, string> Parameters { get; private set; }
            public object[] Arguments { get; private set; }

            public void Started()
            {
                throw new NotImplementedException();
            }
            public void Finish(string result)
            {
                throw new NotImplementedException();
            }
            public void Error(string errorMessage)
            {
                throw new NotImplementedException();
            }
            public void Notify(string message)
            {
                throw new NotImplementedException();
            }
        }

        private IPlugin GetTestablePlugin(Guid key, string name)
        {
            return new TestablePlugin(key, name);
        }

        private class TestablePlugin : IPlugin
        {
            public TestablePlugin(Guid key, string name)
            {
                this.Key = key;
                this.Name = name;
            }

            public Guid Key { get; private set; }
            public string Name { get; private set; }
            public void Execute(IPluginContext context)
            {
                context.Finish("RESULT");
            }
        }
    }*/
}

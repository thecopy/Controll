using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Hubs;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using Ninject;
using SignalR;
using SignalRServer = SignalR.Hosting.Self.Server;

namespace Controll.Hosting 
{
    public class ControllServer
    {
        private readonly SignalRServer signalRServer;

        public ControllServer(string url)
        {
            Bootstrapper.StrapTheBoot();

            signalRServer = new SignalRServer(url, Bootstrapper.NinjectDependencyResolver);
            signalRServer.MapHubs();
        }

        public void Start()
        {
            signalRServer.Start();
        }
    }
}

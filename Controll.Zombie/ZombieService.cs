using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.Authentication;
using Controll.Common.ViewModels;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll
{
    public class ZombieService
    {
        private readonly string _url;
        private string _userName;
        private string _zombieName;
        private ControllZombieClient _client;

        public HubConnection HubConnection { get { return _client.HubConnection; } }

        public string UserName
        {
            get { return _userName; }
        }
        public string ZombieName
        {
            get { return _zombieName; }
        }
        
        public ZombieService(string url)
        {
            _url = url;
        }

        public void Connect(string username, string password, string zombieName)
        {
            var authenticator = new DefaultAuthenticationProvider(_url);
            _client = new ControllZombieClient(authenticator.Connect(username, password, zombieName).Result);
            _client.ActivateZombie += _client_ActivateZombie;
        }

        void _client_ActivateZombie(object sender, ActivityStartedEventArgs e)
        {
            Console.WriteLine("Got activity invocation message!");
            var activity = PluginService.Instance.GetActivityInstance(e.ActivityKey);
            Console.WriteLine("Activity name: " + activity.ViewModel.Name + ", activating...");
            activity.Execute(new DelegateActivityContext(e.ActivityTicket, e.Parameter, e.CommandName, _client));
        }
        
        public bool Register(string userName, string password, string zombieName)
        {
            return _client.Register(userName, password, zombieName);
        }

        public Task Synchronize(List<ActivityViewModel> activitiyVms)
        {
            return _client.Synchronize(activitiyVms);
        }

        public Task SignOut()
        {
            return _client.SignOut();
        }
    }
}

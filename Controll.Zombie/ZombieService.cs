using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.NHibernate;
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

        public void Connect()
        {
            _client = new ControllZombieClient(_url);
        }

        public bool Authenticate(string username, string password, string zombieName)
        {
            var result = _client.LogOn(username, password, zombieName);
            if (result)
            {
                _userName = username;
                _zombieName = zombieName;
            }

            return result;
        }

        /*
        private void ActivateZombie(object sender, ActivityStartedEventArgs e)
        {
            var activityRepo = new GenericRepository<ActivityViewModel>();
            var repo = new GenericRepository<ActivitySessionLog>();

            var activity = activityRepo.Get(e.ActivityKey);

            // Start the actual activity
            var plugin = PluginService.Instance.GetActivityInstance(activity.Key);
            var context = new DelegatePluginContext(e.ActivityTicket, e.Parameter, this._client);
            plugin.Execute(context);

            //Create a log post
            repo.Add(new ActivitySessionLog
                {
                    Activity = activity,
                    Started = DateTime.Now,
                    Ticket = e.ActivityTicket,
                    Parameters = e.Parameter
                });

            // Notify subscribers of the started activity
            OnActivityStarted(e);
        }*/

        public IEnumerable<ActivityViewModel> GetInstalledActivities()
        {
            var activityRepo = new GenericRepository<ActivityViewModel>();
            return activityRepo.GetAll();
        }

        public bool Register(string userName, string password, string zombieName)
        {
            return _client.Register(userName, password, zombieName);
        }
    }
}

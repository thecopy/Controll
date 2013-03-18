using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll
{
    public class ControllClient
    {
        private readonly HubConnection _hubConnection;
        private readonly IHubProxy _hubProxy;

        public event EventHandler<ActivityLogMessageEventArgs> NewActivityLogMessage;
        public event EventHandler<ActivityDownloadedAtZombieEventArgs> ActivityDownloadedAtZombie;

        public void OnActivityDownloadedAtZombie(Guid ticket)
        {
            Console.WriteLine("Recoeved notification of downloaded activity:" + ticket);
            EventHandler<ActivityDownloadedAtZombieEventArgs> handler = ActivityDownloadedAtZombie;
            if (handler != null) handler(this, new ActivityDownloadedAtZombieEventArgs(ticket));
        }

        public void OnNewActivityLogMessage(ActivityLogMessageEventArgs e)
        {
            EventHandler<ActivityLogMessageEventArgs> handler = NewActivityLogMessage;
            if (handler != null) handler(this, e);
        }

        public ControllClient(string url)
        {
            _hubConnection = new HubConnection(url);
            _hubProxy = _hubConnection.CreateHubProxy("clientHub");
            
            SetupEvents();
        }

        public IEnumerable<ActivityViewModel> GetAvaiableActivities()
        {
            return _hubProxy.Invoke<IEnumerable<ActivityViewModel>>("GetAvaiableActivities").Result;
        } 

        public ActivityViewModel GetActivityDetails(Guid activityKey)
        {
            return _hubProxy.Invoke<ActivityViewModel>("GetActivityDetails", activityKey).Result;
        }

        public ControllClient(HubConnection connection, IHubProxy proxy)
        {
            this._hubProxy = proxy;
            this._hubConnection = connection;
        }

        public IEnumerable<ActivityViewModel>GetActivitesInstalledOnZombie(string zombieName)
        {
            return _hubProxy.Invoke<IEnumerable<ActivityViewModel>>("GetActivitesInstalledOnZombie", zombieName).Result;
        } 

        private void SetupEvents()
        {
            this._hubProxy.On<Guid, DateTime, string, ActivityMessageType>("NewActivityMessage",
                            (ticket, time, message, type) =>
                                OnNewActivityLogMessage(new ActivityLogMessageEventArgs(ticket, time, message, type)));

            this._hubProxy.On<Guid>("ActivityDownloadCompleted", guid =>
                {
                    Console.WriteLine("hehe - FICK NOTIFIKATION!!!!!11111");
                    OnActivityDownloadedAtZombie(guid);
                });
        }
        
        public IEnumerable<ZombieViewModel> GetAllZombies()
        {
            return _hubProxy.Invoke<IEnumerable<ZombieViewModel>>("GetAllZombies").Result;
        } 

        public Guid StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters, string commandName)
        {
            return _hubProxy.Invoke<Guid>("StartActivity", zombieName, activityKey, parameters, commandName).Result;
        }

        public Guid DownloadActivityAtZombie(string zombieName, Guid activityKey)
        {
            return _hubProxy.Invoke<Guid>("DownloadActivityAtZombie", zombieName, activityKey).Result;
        }

        public void Connect()
        {
            _hubConnection.Start().Wait();
        }

        public bool LogOn(string userName, string password)
        {
            //Temporary try-catch
            // TODO fix
            try
            {
                _hubProxy["UserName"] = userName;

                _hubProxy.Invoke<bool>("LogOn", password).Wait();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void RegisterUser(string username, string password, string email)
        {
            _hubProxy.Invoke("RegisterUser", username, password, email).Wait();
        }

        public IEnumerable<Tuple<string,string>>GetConnectedClients(string userName)
        {
            return _hubProxy.Invoke<IEnumerable<Tuple<string, string>>>("GetConnectedClients", userName).Result;
        }
    }
}

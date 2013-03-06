using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using SignalR.Client.Hubs;

namespace Controll
{
    public class ControllClient
    {
        private readonly HubConnection hubConnection;
        private readonly IHubProxy hubProxy;

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
            hubConnection = new HubConnection(url);
            hubProxy = hubConnection.CreateProxy("ClientHub");
            
            SetupEvents();
        }

        public IEnumerable<ActivityViewModel> GetAvaiableActivities()
        {
            return hubProxy.Invoke<IEnumerable<ActivityViewModel>>("GetAvaiableActivities").Result;
        } 

        public ActivityViewModel GetActivityDetails(Guid activityKey)
        {
            return hubProxy.Invoke<ActivityViewModel>("GetActivityDetails", activityKey).Result;
        }

        public ControllClient(HubConnection connection, IHubProxy proxy)
        {
            this.hubProxy = proxy;
            this.hubConnection = connection;
        }

        public IEnumerable<ActivityViewModel>GetActivitesInstalledOnZombie(string zombieName)
        {
            return hubProxy.Invoke<IEnumerable<ActivityViewModel>>("GetActivitesInstalledOnZombie", zombieName).Result;
        } 

        private void SetupEvents()
        {
            this.hubProxy.On<Guid, DateTime, string, ActivityMessageType>("NewActivityMessage",
                            (ticket, time, message, type) =>
                                OnNewActivityLogMessage(new ActivityLogMessageEventArgs(ticket, time, message, type)));

            this.hubProxy.On<Guid>("ActivityDownloadCompleted", guid =>
                {
                    Console.WriteLine("hehe - FICK NOTIFIKATION!!!!!11111");
                    OnActivityDownloadedAtZombie(guid);
                });
        }
        
        public IEnumerable<ZombieViewModel> GetAllZombies()
        {
            return hubProxy.Invoke<IEnumerable<ZombieViewModel>>("GetAllZombies").Result;
        } 

        public Guid StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters, string commandName)
        {
            return hubProxy.Invoke<Guid>("StartActivity", zombieName, activityKey, parameters, commandName).Result;
        }

        public Guid DownloadActivityAtZombie(string zombieName, Guid activityKey)
        {
            return hubProxy.Invoke<Guid>("DownloadActivityAtZombie", zombieName, activityKey).Result;
        }

        public void Connect()
        {
            hubConnection.Start().Wait();
        }

        public void LogOn(string userName, string password)
        {
            hubProxy["UserName"] = userName;

            hubProxy.Invoke("LogOn", password).Wait();
        }

        public void RegisterUser(string username, string password, string email)
        {
            hubProxy.Invoke("RegisterUser", username, password, email).Wait();
        }

        public IEnumerable<Tuple<string,string>>GetConnectedClients(string userName)
        {
            return hubProxy.Invoke<IEnumerable<Tuple<string, string>>>("GetConnectedClients", userName).Result;
        }
    }
}

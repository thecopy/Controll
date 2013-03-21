using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.NHibernate;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll
{
    public class ControllZombieClient : IControllPluginClient
    {
        private ZombieState state;

        private readonly HubConnection hubConnection;
        private readonly IHubProxy hubProxy;

        public event EventHandler<ActivityStartedEventArgs> ActivateZombie;
        public event EventHandler<ActivityCompletedEventArgs> ZombieActivityCompleted;
        public event EventHandler<ActivityDownloadOrderEventArgs> ActivityDownloadOrder;



        public ControllZombieClient(string userName, string name)
        {
            hubConnection = new HubConnection("http://localhost:10244");
            hubProxy = hubConnection.CreateHubProxy("ZombieHub");

            hubConnection.Start().Wait();

            hubProxy["BelongsToUser"] = userName;
            hubProxy["ZombieName"] = name;

            SetupEvents();
        }

        #region Events & Event Invocators
        private void SetupEvents()
        {
            hubProxy.On<Guid, Guid, Dictionary<string, string>>("InvokePlugin", OnActivateZombie);
            hubProxy.On<Guid, Guid>("ActivityDownloadOrder", OnActivityDownloadOrder);
            hubProxy.On<Guid>("Ping", OnPing);
        }

        public void OnZombieActivityCompleted(ActivityCompletedEventArgs e)
        {
            EventHandler<ActivityCompletedEventArgs> handler = ZombieActivityCompleted;
            if (handler != null) handler(this, e);
        }

        public void OnActivityDownloadOrder(Guid activityKey, Guid ticket)
        {
            EventHandler<ActivityDownloadOrderEventArgs> handler = ActivityDownloadOrder;
            if (handler != null) handler(this, new ActivityDownloadOrderEventArgs(activityKey, ticket));
        }

        private void OnActivateZombie(Guid activityId, Guid ticket, Dictionary<string, string> parameters)
        {
            EventHandler<ActivityStartedEventArgs> handler = ActivateZombie;
            if (handler != null) handler(this, new ActivityStartedEventArgs(activityId, ticket, parameters));
        }
        #endregion

        public bool LogOn(string userPassword)
        {
            return hubProxy.Invoke<bool>("LogOn", userPassword).Result;
        }

        public IEnumerable<ActivityViewModel> GetAvaiableActivities()
        {
            return hubProxy.Invoke<IEnumerable<ActivityViewModel>>("GetAvaiableActivities").Result;
        }

        public byte[] DownloadActivity(Guid activityKey)
        {
            return hubProxy.Invoke<byte[]>("DownloadActivityBinary", activityKey).Result;
        }

        public ActivityViewModel GetActivityDetails(Guid key)
        {
            return hubProxy.Invoke<ActivityViewModel>("GetActivityDetails", key).Result;
        }

        public IDictionary<Guid, bool> CheckIfSyncingActivitiesIsNeeded(IEnumerable<ActivityViewModel> activities)
        {
            return
                hubProxy.Invoke<IDictionary<Guid, bool>>("IsSyncingActivitiesNeeded",
                    activities.Select(a => new {a.Key, a.Version}))
                        .Result;
        }

        public void OnPing(Guid ticket)
        {
            hubProxy.Invoke("QueueItemDelivered", ticket);
            Console.WriteLine("Ping!");
        }

        #region Zombie Activity Messages
        public virtual void ActivityCompleted(Guid ticket, string result)
        {
            hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Completed, result).Wait();
            OnZombieActivityCompleted(new ActivityCompletedEventArgs(ticket, result));
        }

        public void ActivityError(Guid ticket, string errorMessage)
        {
            hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Failed, errorMessage).Wait();
        }

        public void ActivityNotify(Guid ticket, string notificationMessage)
        {
            hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Notification, notificationMessage).Wait();
        }

        public void ActivityStarted(Guid ticket)
        {
            hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Started, "Started yes indeed").Wait();
        }
        #endregion

        public void NotifyActivityDownloadComplete(Guid ticket)
        {
            Console.WriteLine("Notifying cliens about a finished download");
            hubProxy.Invoke("NotifyActivityDownloadComplete", ticket).Wait();
        }
    }
}

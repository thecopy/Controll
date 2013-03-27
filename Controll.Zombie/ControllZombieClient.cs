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
    public class ControllZombieClient : IControllPluginDelegator
    {
        private readonly HubConnection _hubConnection;
        private readonly IHubProxy _hubProxy;

        public event EventHandler<ActivityStartedEventArgs> ActivateZombie;
        public event EventHandler<ActivityCompletedEventArgs> ZombieActivityCompleted;

        public ControllZombieClient(string url)
        {
            _hubConnection = new HubConnection(url);
            _hubProxy = _hubConnection.CreateHubProxy("ZombieHub");

            _hubConnection.Start().Wait();

            SetupEvents();
        }

        public HubConnection HubConnection      
        {
            get { return _hubConnection; }
        }

        #region Events & Event Invocators
        private void SetupEvents()
        {
            _hubProxy.On<Guid, Guid, IDictionary<string, string>>("InvokeActivity", OnActivatePlugin);
            _hubProxy.On<Guid>("Ping", OnPing);
        }

        public void OnZombieActivityCompleted(ActivityCompletedEventArgs e)
        {
            EventHandler<ActivityCompletedEventArgs> handler = ZombieActivityCompleted;
            if (handler != null) handler(this, e);
        }

        private void OnActivatePlugin(Guid activityId, Guid ticket, IDictionary<string, string> parameters)
        {
            _hubProxy.Invoke("QueueItemDelivered", ticket);
            EventHandler<ActivityStartedEventArgs> handler = ActivateZombie;
            if (handler != null) handler(this, new ActivityStartedEventArgs(activityId, ticket, parameters));
        }
        #endregion

        public bool Register(string userName, string password, string zombieName)
        {
            var result = _hubProxy.Invoke<bool>("RegisterAsZombie", userName, password, zombieName).Result;
            if (result)
            {
                _hubProxy["BelongsToUser"] = userName;
                _hubProxy["ZombieName"] = zombieName;
            }

            return result;

        }

        public bool LogOn(string userName, string password, string zombieName)
        {
            var result = _hubProxy.Invoke<bool>("LogOn", userName, password, zombieName).Result; 
            if (result)
            {
                _hubProxy["BelongsToUser"] = userName;
                _hubProxy["ZombieName"] = zombieName;
            }

            return result;
        }
        
        public void OnPing(Guid ticket)
        {
            _hubProxy.Invoke("QueueItemDelivered", ticket);
            Console.WriteLine("Ping!");
        }

        #region Zombie Activity Messages
        public virtual void ActivityCompleted(Guid ticket, string result)
        {
            _hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Completed, result).Wait();
            OnZombieActivityCompleted(new ActivityCompletedEventArgs(ticket, result));
        }

        public void ActivityError(Guid ticket, string errorMessage)
        {
            _hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Failed, errorMessage).Wait();
        }

        public void ActivityNotify(Guid ticket, string notificationMessage)
        {
            _hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Notification, notificationMessage).Wait();
        }

        public void ActivityStarted(Guid ticket)
        {
            _hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Started, "Started yes indeed").Wait();
        }
        #endregion

        public void Synchronize(List<ActivityViewModel> activitiyVms)
        {
            _hubProxy.Invoke("SynchronizeActivities", activitiyVms);
        }
    }
}

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
    public class ControllZombieClient : IActivityDelegator
    {
        private readonly HubConnection _hubConnection;
        private readonly IHubProxy _hubProxy;

        public event EventHandler<ActivityStartedEventArgs> ActivateZombie;
        public event EventHandler<ActivityCompletedEventArgs> ZombieActivityCompleted;
        public event EventHandler<PingEventArgs> Pinged;

        public ControllZombieClient(string url)
        {
            _hubConnection = new HubConnection(url);
            _hubProxy = _hubConnection.CreateHubProxy("ZombieHub");

            SetupEvents();
            _hubConnection.Start().Wait();
        }

        public HubConnection HubConnection      
        {
            get { return _hubConnection; }
        }

        #region Events & Event Invocators
        private void SetupEvents()
        {
            _hubProxy.On<Guid, Guid, IDictionary<string, string>, string>("InvokeActivity", OnActivatePlugin);
            _hubProxy.On<Guid>("Ping", OnPing);
        }

        protected virtual void OnPinged(PingEventArgs e)
        {
            EventHandler<PingEventArgs> handler = Pinged;
            if (handler != null) handler(this, e);
        }

        public void OnZombieActivityCompleted(ActivityCompletedEventArgs e)
        {
            EventHandler<ActivityCompletedEventArgs> handler = ZombieActivityCompleted;
            if (handler != null) handler(this, e);
        }

        private void OnActivatePlugin(Guid activityId, Guid ticket, IDictionary<string, string> parameters, string commandName)
        {
            _hubProxy.Invoke("QueueItemDelivered", ticket).ContinueWith(t =>
                {
                    EventHandler<ActivityStartedEventArgs> handler = ActivateZombie;
                    if (handler != null) handler(this, new ActivityStartedEventArgs(activityId, ticket, parameters, commandName));
                });
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
        
        private void OnPing(Guid ticket)
        {
            Console.WriteLine("Ping!");
            OnPinged(new PingEventArgs(ticket));
            _hubProxy.Invoke("QueueItemDelivered", ticket).Wait();
        }

        #region Zombie Activity Messages

        public void ActivityResult(Guid ticket, object result)
        {
            _hubProxy.Invoke("ActivityResult", ticket, result).Wait();
        }

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
            _hubProxy.Invoke("ActivityMessage", ticket, ActivityMessageType.Started, "").Wait();
        }
        #endregion

        public Task Synchronize(List<ActivityViewModel> activitiyVms)
        {
            return _hubProxy.Invoke("SynchronizeActivities", activitiyVms);
        }

        public Task SignOut()
        {
            return _hubProxy.Invoke("SignOut");
        }
    }
}

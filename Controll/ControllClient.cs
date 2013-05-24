using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Controll.Client.Models;
using Controll.Common;
using Controll.Common.ViewModels;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll.Client
{
    public class ControllClient
    {
        private readonly HubConnection _hubConnection;
        private readonly IHubProxy _hubProxy;

        public event EventHandler<MessageDeliveredEventArgs> MessageDelivered;
        public event EventHandler<ActivityLogMessageEventArgs> ActivityMessageRecieved;
        public event EventHandler<ActivityResultEventArgs> ActivityResultRecieved;
        
        public HubConnection HubConnection { get { return _hubConnection; } }
        
        private void OnMessageDelivered(Guid ticket)
        {
            var handler = MessageDelivered;
            if (handler != null)
                handler(this, new MessageDeliveredEventArgs(ticket));
        }

        private void OnActivityMessage(Guid ticket, ActivityMessageType type, string message)
        {
            var handler = ActivityMessageRecieved;
            if (handler != null) handler(this, new ActivityLogMessageEventArgs(ticket, message, type));
        }

        private void OnActivityResult(Guid ticket, object result)
        {
            var handler = ActivityResultRecieved;
            if (handler != null) handler(this, new ActivityResultEventArgs(ticket, result));
        }

        public ControllClient(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
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
            _hubProxy.On<Guid>("MessageDelivered", OnMessageDelivered);
            _hubProxy.On<Guid, ActivityMessageType, string>("ActivityMessage", OnActivityMessage);
            _hubProxy.On<Guid, object>("ActivityResult", OnActivityResult);
        }

        public IEnumerable<ZombieViewModel> GetAllZombies()
        {
            return _hubProxy.Invoke<IEnumerable<ZombieViewModel>>("GetAllZombies").Result;
        } 

        public Guid StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters, string commandName)
        {
            return _hubProxy.Invoke<Guid>("StartActivity", zombieName, activityKey, parameters, commandName).Result;
        }

        public Guid Ping(string zombieName)
        {
            var ticket = _hubProxy.Invoke<Guid>("PingZombie", zombieName).Result;

            return ticket;
        }

        public Guid DownloadActivityAtZombie(string zombieName, Guid activityKey)
        {
            return _hubProxy.Invoke<Guid>("DownloadActivityAtZombie", zombieName, activityKey).Result;
        }

        public Task<bool> LogOnAsync(string userName, string password)
        {
            _hubProxy["userName"] = userName;
            return _hubProxy.Invoke<bool>("LogOn", password);
        }

        public Task SignIn()
        {
            return _hubConnection.Start().ContinueWith(_ =>
                {
                    if (_.IsFaulted)
                    {
                        if (_.Exception != null)
                            throw _.Exception;
                        throw new Exception("Could not connect to server");
                    }

                    _hubProxy.Invoke("SignIn");
                });
        }

        public bool RegisterUser(string username, string password, string email)
        {
            return _hubProxy.Invoke<bool>("RegisterUser", username, password, email).Result;
        }

        public void Disconnect()
        {
            _hubConnection.Stop();
        }
    }
}

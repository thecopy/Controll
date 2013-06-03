using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Controll.Client.Models;
using Controll.Common;
using Controll.Common.ViewModels;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll.Client
{
    public class ControllClient : IControllClient
    {
        private readonly HubConnection _hubConnection;
        private readonly IHubProxy _hubProxy;

        public event EventHandler<MessageDeliveredEventArgs> MessageDelivered;
        public event EventHandler<ActivityLogMessageEventArgs> ActivityMessageRecieved;
        public event EventHandler<ActivityResultEventArgs> ActivityResultRecieved;
        
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

        private void SetupEvents()
        {
            _hubProxy.On<Guid>("MessageDelivered", OnMessageDelivered);
            _hubProxy.On<Guid, ActivityMessageType, string>("ActivityMessage", OnActivityMessage);
            _hubProxy.On<Guid, object>("ActivityResult", OnActivityResult);
        }

        public Task<IEnumerable<ZombieViewModel>> GetAllZombies()
        {
            return _hubProxy.Invoke<IEnumerable<ZombieViewModel>>("GetAllZombies");
        } 

        public Task<Guid> StartActivity(
            string zombieName, 
            Guid activityKey, 
            Dictionary<string, string> parameters, 
            string commandName)
        {
            return _hubProxy.Invoke<Guid>("StartActivity", zombieName, activityKey, parameters, commandName);
        }

        public Task<Guid> Ping(string zombieName)
        {
            return _hubProxy.Invoke<Guid>("PingZombie", zombieName);
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

                    SignIn();
                });
        }

        public Task AddZombie(string zombieName)
        {
            return _hubProxy.Invoke("AddZombie", zombieName);
        }

        public Task<IEnumerable<LogBookViewModel>> GetLogBooks(int take, int skip)
        {
            return _hubProxy.Invoke<IEnumerable<LogBookViewModel>>("GetLogBooks", take, skip);
        }

        public void Disconnect()
        {
            _hubConnection.Stop();
        }
    }
}

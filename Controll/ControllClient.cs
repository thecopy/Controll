using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Controll.Client.Authentication;
using Controll.Client.Models;
using Controll.Common;
using Controll.Common.Authentication;
using Controll.Common.Helpers;
using Controll.Common.ViewModels;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll.Client
{
    public class ControllClient : IControllClient
    {
        private readonly string _url;
        private readonly IAuthenticationProvider _authenticationProvider;
        private HubConnection _hubConnection;
        private IHubProxy _hubProxy;

        public event EventHandler<MessageDeliveredEventArgs> MessageDelivered;
        public event EventHandler<ActivityLogMessageEventArgs> ActivityMessageRecieved;
        public event EventHandler<ActivityResultEventArgs> ActivityResultRecieved;
        public event Action<String, IEnumerable<ActivityViewModel>> ZombieSynchronized;

        public string Url
        {
            get { return _url; }
        }

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

        public ControllClient(string url, IAuthenticationProvider authenticationProvider)
        {
            if(authenticationProvider == null)
                throw new ArgumentNullException("authenticationProvider");

            _url = url;
            _authenticationProvider = authenticationProvider;
        }

        public ControllClient(string url) 
            : this(url, new DefaultAuthenticationProvider(url)) {}
        
        public Task Connect(string username, string password)
        {
              return _authenticationProvider
                .Connect(username, password, null)
                .Then(connection =>
                    {
                        _hubConnection = connection;
                        _hubProxy = connection.CreateHubProxy("ClientHub");
                        
                        SetupEvents();

                        return _hubConnection.Start();
                    })
                .Then(() => SignIn());
        }

        private Task SignIn()
        {
            return _hubProxy.Invoke("SignIn");
        }

        private void SetupEvents()
        {
            _hubProxy.On<Guid>("MessageDelivered", OnMessageDelivered);
            _hubProxy.On<Guid, ActivityMessageType, string>("ActivityMessage", OnActivityMessage);
            _hubProxy.On<Guid, object>("ActivityResult", OnActivityResult);

            _hubProxy.On<String, IEnumerable<ActivityViewModel>>("ZombieSynchronized", (name, activities) =>
                {
                    if (ZombieSynchronized != null)
                        ZombieSynchronized(name, activities);
                });
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

        public Task<Guid> DownloadActivity(string zombieName, string url)
        {
            return _hubProxy.Invoke<Guid>("DownloadActivity", zombieName, url);
        }

        public Task AddZombie(string zombieName)
        {
            return _hubProxy.Invoke("AddZombie", zombieName);
        }

        public Task<IEnumerable<LogBookViewModel>> GetLogBooks(int take, int skip)
        {
            return _hubProxy.Invoke<IEnumerable<LogBookViewModel>>("GetLogBooks", take, skip);
        }

        public void Stop()
        {
            _hubProxy.Invoke("SignOut").Wait();
            _hubConnection.Stop();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Controll.Client.Authentication;
using Controll.Common;
using Controll.Common.Authentication;
using Controll.Common.Helpers;
using Controll.Common.ViewModels;
using Controll.Zombie.Infrastructure;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Controll.Client
{
    public class ZombieClient : IZombieClient
    {
        private readonly string _url;
        private readonly IAuthenticationProvider _authentication;

        private IHubProxy _hubProxy;
        private HubConnection _hubConnection;
        
        public HubConnection HubConnection
        {
            get { return _hubConnection; }
        }
        public string Url
        {
            get { return _url; }
        }

        public event Action<Guid, string> DownloadActivityRequest;
        public event Action<InvocationInformation> InvocationRequest;
        public event Action<Guid> Pinged;

        public Task Connect(string username, string password, string zombieName)
        {
            return _authentication
                .Connect(username, password, zombieName)
                .Then(connection =>
                    {
                        _hubConnection = connection;
                        _hubProxy = connection.CreateHubProxy("ZombieHub");
                        
                        SetupEvents();

                        return HubConnection.Start();
                    })
                .Then(() => SignIn());
        }

        public ZombieClient(string url, IAuthenticationProvider authentication = null)
        {
            _url = url;
            _authentication = authentication ?? new DefaultAuthenticationProvider(Url);
        }

        private void SetupEvents()
        {
            _hubProxy.On<Guid, Guid, IDictionary<string, string>, string>("InvokeActivity",
                (activityKey, ticket, parameters, commandName) =>
                    {
                        if(InvocationRequest != null)
                            InvocationRequest(new InvocationInformation(activityKey, ticket, parameters, commandName));
                    });

            _hubProxy.On<Guid>("Ping", ticket =>
                {
                    if(Pinged != null)
                        Pinged(ticket);
                });

            _hubProxy.On<Guid, String>("DownloadActivity", (ticket, url) =>
            {
                if (DownloadActivityRequest != null)
                    DownloadActivityRequest(ticket, url);
            });
        }

        private Task SignIn()
        {
            return _hubProxy.Invoke("SignIn");
        }
        
        public Task ConfirmMessageDelivery(Guid ticket)
        {
            return _hubProxy.Invoke("QueueItemDelivered", ticket);
        }
        
        public Task Synchronize(IEnumerable<ActivityViewModel> activitiyVms)
        {
            return _hubProxy.Invoke("SynchronizeActivities", activitiyVms);
        }

        public Task SignOut()
        {
            return _hubProxy.Invoke("SignOut");
        }

        public void ActivityResult(Guid ticket, object result)
        {
            _hubProxy.Invoke("ActivityResult", ticket, result).Wait();
        }

        public void ActivityMessage(Guid ticket, ActivityMessageType type, string message = "")
        {
            _hubProxy.Invoke("ActivityMessage", ticket, type, message).Wait();
        }
    }
}

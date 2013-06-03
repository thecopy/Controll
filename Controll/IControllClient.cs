using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Controll.Client.Models;
using Controll.Common.ViewModels;

namespace Controll.Client
{
    public interface IControllClient
    {
        event EventHandler<MessageDeliveredEventArgs> MessageDelivered;
        event EventHandler<ActivityLogMessageEventArgs> ActivityMessageRecieved;
        event EventHandler<ActivityResultEventArgs> ActivityResultRecieved;

        Task SignIn();
        void Disconnect();

        Task AddZombie(string zombieName);

        Task<IEnumerable<ZombieViewModel>> GetAllZombies();
        Task<IEnumerable<LogBookViewModel>> GetLogBooks(int take, int skip);
        
        Task<Guid> StartActivity(string zombieName, Guid activityKey, Dictionary<string, string> parameters, string commandName);
        Task<Guid> Ping(string zombieName);
    }
}
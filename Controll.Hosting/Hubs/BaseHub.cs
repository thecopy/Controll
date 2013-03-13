using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Models;
using Controll.Hosting.Repositories;
using Controll.Hosting.Services;
using SignalR.Hubs;

namespace Controll.Hosting.Hubs
{
    public class BaseHub : Hub
    {
        private readonly IGenericRepository<Activity> _activityRepository;

        public BaseHub(IGenericRepository<Activity> activityRepository)
        {
            _activityRepository = activityRepository;
        }
    }
}

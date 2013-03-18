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
using Microsoft.AspNet.SignalR;
using NHibernate;

namespace Controll.Hosting.Hubs
{
    public class BaseHub : Hub
    {
        protected readonly ISession Session;

        public BaseHub(ISession session)
        {
            this.Session = session;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using NHibernate;

namespace Controll.Hosting.Infrastructure
{
    public class ControllHostingConfiguration
    {
        private string _hubScope = "hub";
        public string HubScope
        {
            get { return _hubScope; }
            set { _hubScope = value; }
        }

        public string ConnectionStringAlias { get; set; }
        public bool ClearDatabase { get; set; }
        public bool UseCustomSessionFactory { get; set; }
        public ISessionFactory CustomSessionFactory { get; set; }

        public bool DenyAnonymous { get; set; }
        public bool RedirectToLoginRoute { get; set; }
        public String LoginRoute { get; set; }

        public HubConfiguration HubConfiguration { get; set; }

        public bool IsValid
        {
            get
            {
                if (UseCustomSessionFactory)
                {
                    if (CustomSessionFactory == null)
                        return false;

                    if (!string.IsNullOrEmpty(ConnectionStringAlias))
                        return false;
                }

                if (!UseCustomSessionFactory)
                {
                    if (CustomSessionFactory != null)
                        return false;

                    if (string.IsNullOrEmpty(ConnectionStringAlias))
                        return false;
                }

                if (string.IsNullOrEmpty(HubScope))
                    return false;

                if (RedirectToLoginRoute && string.IsNullOrEmpty(LoginRoute))
                    return false;

                return true;
            }
        }
    }
}

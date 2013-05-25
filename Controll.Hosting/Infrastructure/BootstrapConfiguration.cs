using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;

namespace Controll.Hosting.Infrastructure
{
    public class BootstrapConfiguration
    {
        private string _hubScope = "hub";
        public string ConnectionStringAlias { get; set; }
        public bool ClearDatabase { get; set; }
        public bool UseCustomSessionFactory { get; set; }
        public ISessionFactory CustomSessionFactory { get; set; }
        public string HubScope
        {
            get { return _hubScope; }
            set { _hubScope = value; }
        }

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

                return true;
            }
        }
    }
}

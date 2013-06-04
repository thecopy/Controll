using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Infrastructure
{
    public interface IApplicationSettings
    {
        string LocalStoragePath { get; }
    }

    public class ApplicationSettings : IApplicationSettings
    {
        public string LocalStoragePath
        {
            get { return ConfigurationManager.AppSettings["controll:localstorage:path"]; }
        }
    }
}

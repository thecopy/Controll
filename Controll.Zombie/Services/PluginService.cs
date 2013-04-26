using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Controll.Common;

namespace Controll
{
    public sealed class PluginService
    {
        #region Singleton
        PluginService()
        {
            PluginPath = @".\";
        }

        public static PluginService Instance
        {
            get { return Nested.instance; }
        }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly PluginService instance = new PluginService();
        }
        #endregion

        public string PluginPath { get; set; }

        public IActivity GetActivityInstance(Guid activityKey)
        {
            var activityType = GetAllActivityTypes().SingleOrDefault(p =>
                                   p.GetCustomAttribute<ActivityAttribute>().Key == activityKey);

            if (activityType == null)
                throw new ArgumentException("There exists no activities with the specified key", "activityKey");

            var activity = (IActivity)Activator.CreateInstance(activityType);

            return activity;
        }

        public IEnumerable<Type> GetAllActivityTypes()
        {
            var assemblies = new DirectoryInfo(".\\").GetFiles("*.plugin.dll");
            foreach (var assembly in assemblies)
            {
                var a = Assembly.LoadFrom(assembly.FullName);
                foreach (Type type in a.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IActivity))))
                {
                    yield return type;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.SampleZombie
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private ZombieService client;

        private string log;
        public string Log
        {
            get { return log; }
            set { log = value; OnPropertyChanged("Log");}
        }
        private void AddLog(string what)
        {
            Log = what + "\r\n" + Log;
        }

        public void Connect(string userName, string zombieName)
        {
            AddLog("Connecting");
            client = new ZombieService(userName, zombieName);
            client.ActivityStarted += client_ActivityStarted;
                
            AddLog("OK");
        }

        void client_ActivityStarted(object sender, ActivityStartedEventArgs e)
        {
            AddLog("Activity " + e.ActivityKey + " started with parameters " +string.Join(", ", e.Parameter.Select(p => p.Key + "=" + p.Value)));
        }

        public void LogOn(string password)
        {
            AddLog("Logging in...");
            var result = client.Start(password);
            AddLog("Log in: " + (result ? "OK" : "Failed"));
        }

        public void GetAllActivities()
        {
            AddLog("Fetching all activities avaiable");
            var activities = client.GetAvaiableActivities();

            AddLog("Done, count: " + activities.Count());
            foreach (var activityVm in activities)
            {
                AddLog(" * " + activityVm.Name);
            }
        }

        public void DownloadActivity(Guid key)
        {
            var activity = client.GetAvaiableActivities().Single(a => a.Name.Contains("Spotify"));
            long count = client.DownloadActivity(activity);

            AddLog("Downloaded " + count / 1024 + " kB");
        }
        internal void ListAllInstalledActivities()
        {
            foreach (var activityVm in client.GetInstalledActivities())
            {
                AddLog(string.Format(" * {0} by {1}. Version {2}", activityVm.Name, activityVm.CreatorName, activityVm.Version));
            }
        }

        public void PrintStatus()
        {
            var installedActivities = client.GetInstalledActivities();
            var avaiableActivities = client.GetAvaiableActivities();

            AddLog("------------");
            foreach(var activity in avaiableActivities)
                AddLog(string.Format(" * {0} by {1}. Version {2}", activity.Name, activity.CreatorName, activity.Version));
            AddLog("Available Activities:");

            AddLog("------------");
            foreach (var activity in installedActivities)
                AddLog(string.Format(" * {0} by {1}. Version {2}", activity.Name, activity.CreatorName, activity.Version));
            AddLog("Installed Activities:");

            AddLog(string.Format("Zombie Name: {0}\n UserName: {1}", client.ZombieName, client.UserName));
        }
    }
}

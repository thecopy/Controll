using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Controll.Common;

namespace Controll.SampleClient
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string userName;
        private ControllClient client;
        private string status;
        private string log;

        public string Parameter { get; set; }

        public Dictionary<Guid, ActivityViewModel> DownloadOrders { get; private set; }

        public string Status
        {
            get { return status; }
            set { status = value; OnPropertyChanged("Status"); }
        }
        public string Log
        {
            get { return log; }
            set { log = value; OnPropertyChanged("Log"); }
        }

        public string KeyToDownload { get; set; }

        public string KeyToStart { get; set; }

        public MainWindowViewModel()
        {
            DownloadOrders = new Dictionary<Guid, ActivityViewModel>();
            KeyToDownload = "27611fad-17cd-463b-a179-796f3e3b1121";
        }

        private void AddToLog(string what)
        {
            Status = what;
            Log = what + "\n" + Log;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }


        public void Connect(string url)
        {
            AddToLog("Trying to connect to " + url);
            client = new ControllClient(url);
            client.Connect();

            client.NewActivityLogMessage += client_NewActivityLogMessage;
            client.ActivityDownloadedAtZombie += client_ActivityDownloadedAtZombie;
            
            AddToLog("Connected to " + url);
        }

        void client_ActivityDownloadedAtZombie(object sender, ActivityDownloadedAtZombieEventArgs e)
        {
            var request = DownloadOrders[e.Ticket];
            AddToLog("Activity downloaded at zombie: " + request.Name);
            DownloadOrders.Remove(e.Ticket);
        }

        void client_NewActivityLogMessage(object sender, ActivityLogMessageEventArgs e)
        {
            AddToLog("Message: " + e.Type + ": " + e.Message);
        }

        public void LogOn(string username, string password, Guid device)
        {
            try
            {
                this.userName = username;
                AddToLog("Logging in...");
                client.LogOn(username, password);
                AddToLog("Logged in");
            }catch(Exception e)
            {
                AddToLog("Error while logging in: " + e.GetBaseException().Message);
            }
        }

        public void PrintAllAvaiableActivities()
        {
            var s = new StringBuilder("All Activities on the Cloud:");
            foreach(var vm in client.GetAvaiableActivities())
            {
                s.AppendFormat("{0} by {1}, last updated {2}. Key: {3}\n", vm.Name, vm.CreatorName,
                               vm.LastUpdated.ToShortDateString(), vm.Key);
            }

            AddToLog(s.ToString());
        }

        public void Register(string username,string password)
        {
            this.userName = username;
            AddToLog("Register user " + username);
            client.RegisterUser(username, password, "TEMPMAIL");
            AddToLog("Register OK");
        }

        public void GetConnectedClients()
        {
            AddToLog("Fetching " + userName + "'s connected clients");

            client.GetConnectedClients(userName).ToList()
                .ForEach(cTuple => AddToLog(cTuple.Item2 + ": " + cTuple.Item1));
        }

        public void DownloadActivityAtZombie(string zombieName, Guid key)
        {
            var ticket = client.DownloadActivityAtZombie(zombieName, key);
            var activityDetails = client.GetActivityDetails(key);

            DownloadOrders.Add(ticket, activityDetails);

        }

        public void ActivateZombie(string zombieName, Guid id, string parameter)
        {
            AddToLog("Registring this computer as a zombie");

            var result = client.StartActivity(zombieName, id, new Dictionary<string, string> { { "name", "walk this way" } },
                                              "findtrack");
 
            AddToLog("Succesful! Ticket: " + result);
        }

        public void PrintActivitesInstalledOnZombie(string zombieName)
        {
            var s = new StringBuilder("All Activities installed on zombie " + zombieName + ":");
            foreach (var vm in client.GetActivitesInstalledOnZombie(zombieName))
            {
                s.AppendFormat("{0} by {1}, last updated {2}. Key: {3}\n", vm.Name, vm.CreatorName,
                               vm.LastUpdated.ToShortDateString(), vm.Key);
            }

            AddToLog(s.ToString());
        }
    }
}

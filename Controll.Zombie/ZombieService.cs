using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.NHibernate;

namespace Controll
{
    public class ZombieService
    {
        private readonly string userName;
        private readonly string zombieName;
        private readonly string activitiesPath;
        private ControllZombieClient client;

        public event EventHandler<ActivityStartedEventArgs> ActivityStarted;

        public string UserName
        {
            get { return userName; }
        }
        public string ZombieName
        {
            get { return zombieName; }
        }

        private List<ActivityDownloadOrderEventArgs> DownloadRequests { get; set; }   

        public void OnActivityStarted(ActivityStartedEventArgs e)
        {
            EventHandler<ActivityStartedEventArgs> handler = ActivityStarted;
            if (handler != null) handler(this, e);
        }


        public ZombieService(string userName, string zombieName, string activitiesPath = ".")
        {
            this.userName = userName;
            this.zombieName = zombieName;
            this.activitiesPath = activitiesPath;
            this.DownloadRequests = new List<ActivityDownloadOrderEventArgs>();
        }

        public bool Start(string password)
        {
            client = new ControllZombieClient(userName, zombieName);
            client.ActivateZombie += ActivateZombie;
            client.ActivityDownloadOrder += client_ActivityDownloadOrder;

            return client.LogOn(password);
        }

        void client_ActivityDownloadOrder(object sender, ActivityDownloadOrderEventArgs e)
        {
            Console.WriteLine("Downloading activity on behalf of client");
            this.DownloadRequests.Add(e);
            this.DownloadActivity(e.ActivityKey);
            // TODO: Find out if ticket is needed here
        }

        private void ActivateZombie(object sender, ActivityStartedEventArgs e)
        {
            var activityRepo = new GenericRepository<ActivityViewModel>();
            var repo = new GenericRepository<ActivitySessionLog>();

            var activity = activityRepo.Get(e.ActivityKey);

            // Start the actual activity
            var plugin = PluginService.Instance.GetActivityInstance(activity.Key);
            var context = new DelegatePluginContext(e.ActivityTicket, e.Parameter, this.client);
            plugin.Execute(context);

            //Create a log post
            repo.Add(new ActivitySessionLog
                {
                    Activity = activity,
                    Started = DateTime.Now,
                    Ticket = e.ActivityTicket,
                    Parameters = e.Parameter
                });

            // Notify subscribers of the started activity
            OnActivityStarted(e);
        }
        
        public IEnumerable<ActivityViewModel> GetAvaiableActivities()
        {
            return client.GetAvaiableActivities();
        }

        public IEnumerable<ActivityViewModel> GetInstalledActivities()
        {
            var activityRepo = new GenericRepository<ActivityViewModel>();
            return activityRepo.GetAll();
        }

        public long DownloadActivity(Guid key)
        {
            var repo = new GenericRepository<ActivityViewModel>();

            // Download the activity
            var bytes = client.DownloadActivity(key);
            if (bytes.Length > 0)
            {
                //Save it
                File.WriteAllBytes(Path.Combine(activitiesPath, key.ToString() + ".activity"), bytes);

                //Add the details of the activity in the database
                var details = repo.Get(key);
                if (details == null)
                {
                    repo.Add(GetActivityDetails(key));
                }
                else
                {
                    details = GetActivityDetails(key);
                    repo.Update(details);
                }
            }
            Console.WriteLine("Downloaded activity successfully");
            ActivityDownloadOrderEventArgs request = this.DownloadRequests.SingleOrDefault(dr => dr.ActivityKey == key);
            if(request != null)
            {
                client.NotifyActivityDownloadComplete(request.Ticket);
                this.DownloadRequests.Remove(request);
            }

            return bytes.Length;
        }

        public long DownloadActivity(ActivityViewModel activity)
        {
            return this.DownloadActivity(activity.Key);
        }

        public ActivityViewModel GetActivityDetails(Guid key)
        {
            return client.GetActivityDetails(key);
        }

        public IDictionary<Guid, bool> CheckIfSyncingActivitiesIsNeeded()
        {
            throw new NotImplementedException();
        }

    }
}

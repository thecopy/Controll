using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll;
using Controll.Zombie;
using Controll.Common;
using Controll.Common.ViewModels;

namespace SimpleConsoleZombie
{
    class Program
    {
        private static ZombieClient _service;
        static void Main(string[] args)
        {
            Console.WriteLine("Simple Console Zombie for Controll");
            Console.WriteLine("https://github.com/thecopy/Controll");

            Console.WriteLine("Connect to localhost:10244 (Y/n)?");

            var result = Console.ReadLine();
            if (string.IsNullOrEmpty(result) || result.ToLower() == "y")
            {
                Connect("http://localhost:10244/");
            }

            Console.WriteLine("Connect to localhost:<port> (Y/n)?");
            result = Console.ReadLine();
            if (string.IsNullOrEmpty(result) || result.ToLower() == "y")
            {
                Console.Write("Enter port: ");
                string port = Console.ReadLine();
                Connect("http://localhost:" + int.Parse(port) + "/");
            }
            else
            {
                Console.Write("Enter url: ");
                string url = Console.ReadLine();
                Connect(url);
            }
        }

        private static void Connect(string url)
        {
            Console.WriteLine("Initializing service...");
            _service = new ZombieClient(url);
            Console.WriteLine("Connecting to " + url);

            Console.Write("Provide username('username'): ");
            var username = Console.ReadLine();
            if (string.IsNullOrEmpty(username)) username = "username";
            Console.Write("Provide password('password'): ");
            var password = Console.ReadLine();
            if (string.IsNullOrEmpty(password)) password = "password";
            Console.Write("Provide zombie name('zombieName'): ");
            var zombieName = Console.ReadLine();
            if (string.IsNullOrEmpty(password)) zombieName = "zombieName";

            _service.Connect(username, password, zombieName).Wait();
            

            Console.WriteLine("Connected!");
            Console.WriteLine("Type h or help for more information");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("> ");
            string result = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Gray;

            do
            {
                if (result == null) continue;
                var results = result.Split(' ');
                switch (results[0].ToLower())
                {
                    case "help":
                    case "h":
                        Console.WriteLine("status\t\tPrint status");
                        Console.WriteLine("auth\t\tAuthenticate");
                        Console.WriteLine("list\t\tDisplay list of different types");
                        Console.WriteLine("register\t\tRegister as zombie to user");
                        Console.WriteLine("sync\t\tSync server activity list for this zombie");
                        break;
                    case "signout":
                        SignOut();
                        break;
                    case "list":
                        if(results.Count() >= 2)
                        List(results[1], results.Skip(2).ToArray());
                        break;
                    case "sync":
                        Sync();
                        break;
                    case "status":
                        PrintStatus();
                        break;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("> ");
                result = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            } while (string.IsNullOrEmpty(result) || result.ToLower() != "q" || result.ToLower() != "quit");
        }

        private static void SignOut()
        {
            _service.SignOut().Wait();
            Console.WriteLine("Signed out!");
        }

        private static void Sync()
        {
            var activitiyTypes = PluginService.Instance.GetAllActivityTypes().ToList();
            var activitiyVms = new List<ActivityViewModel>(activitiyTypes.Count);
            activitiyVms.AddRange(activitiyTypes.Select(activity => 
                (IActivity) Activator.CreateInstance(activity)).Select(activityInstance => 
                    activityInstance.ViewModel));

            _service.Synchronize(activitiyVms).Wait();
            Console.WriteLine("Ok. Synchronized!");
        }

        static private void List(string what, params string[] parameters)
        {
            if (what == "activities")
            {
                var activities = PluginService.Instance.GetAllActivityTypes().ToList();
                foreach (var activity in activities)
                {
                    Console.WriteLine(activity.FullName);
                }
                Console.WriteLine("Total: {0} activities avaiable.", activities.Count);
            }
            else
            {
                Console.WriteLine("Avaiable enumerables: activities");
            }
        }

        private static void PrintStatus()
        {
            Console.WriteLine("Connected to: {0}", _service.HubConnection.Url);
            Console.WriteLine("Transport: {0}", _service.HubConnection.Transport.Name);
            Console.WriteLine("Connection-Id: {0}", _service.HubConnection.ConnectionId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Controll;
using Controll.Client;
using Controll.Common;
using Controll.Common.Helpers;
using Controll.Common.ViewModels;
using Controll.Zombie.Infrastructure;

namespace SimpleConsoleZombie
{
    class Program
    {
        private static ZombieClient _client;
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

            _client = new ZombieClient(url);
            _client.Pinged += _client_Pinged;
            _client.InvocationRequest += _client_InvocationRequest;
            _client.DownloadActivityRequest += _client_DownloadActivityRequest;

            Console.WriteLine("Connecting to " + url);

            Console.Write("Provide username('username'): ");
            var username = Console.ReadLine();
            if (string.IsNullOrEmpty(username)) username = "username";
            Console.Write("Provide password('password'): ");
            var password = Console.ReadLine();
            if (string.IsNullOrEmpty(password)) password = "password";
            Console.Write("Provide zombie name('zombieName'): ");
            var zombieName = Console.ReadLine();
            if (string.IsNullOrEmpty(zombieName)) zombieName = "zombieName";

            _client.Connect(username, password, zombieName).Wait();

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
                        Console.WriteLine("sync\t\tSync server activity list for this zombie");
                        break;
                    case "sync":
                        Sync();
                        break;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("> ");
                result = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            } while (string.IsNullOrEmpty(result) || result.ToLower() != "q" || result.ToLower() != "quit");

            SignOut();
        }

        private static void _client_DownloadActivityRequest(Guid ticket, string url)
        {
            _client.ConfirmMessageDelivery(ticket);

            Console.WriteLine("Downloading activity from '{0}'...", url);

            var fileName = (Path.GetFileNameWithoutExtension(url) ?? Guid.NewGuid().ToString("N")) + ".plugin.dll";
            var savePath = Path.Combine(Environment.CurrentDirectory, fileName);
            Console.WriteLine("Saving activity to {0}...", savePath);

            new WebClient().DownloadFile(url, savePath);

            Console.WriteLine("OK! Syncing...");
            Sync();
        }

        static void _client_InvocationRequest(InvocationInformation info)
        {
            try
            {
                _client.ConfirmMessageDelivery(info.Ticket);

                var activity = PluginService.Instance.GetActivityInstance(info.ActivityKey);
                Console.WriteLine("Starting activity '{0}'.{1}", activity.ViewModel.Name, info.CommandName);
                activity.Execute(new DelegateActivityContext(info.Ticket, info.Parameter, info.CommandName, _client));

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting activity: " + ex.Message);
                _client.ActivityMessage(info.Ticket, ActivityMessageType.Failed, ex.Message);
            }
        }

        static void _client_Pinged(Guid ticket)
        {
            Console.WriteLine("Ping!");
            _client.ConfirmMessageDelivery(ticket);
        }

        private static void SignOut()
        {
            _client.SignOut().Wait();
            Console.WriteLine("Signed out!");
        }

        private static void Sync()
        {
            var activitiyTypes = PluginService.Instance.GetAllActivityTypes().ToList();
            var activitiyVms = new List<ActivityViewModel>(activitiyTypes.Count);
            activitiyVms.AddRange(activitiyTypes.Select(activity => 
                (IActivity) Activator.CreateInstance(activity)).Select(activityInstance => 
                    activityInstance.ViewModel));

            try
            {
                _client.Synchronize(activitiyVms).Wait();
                Console.WriteLine("Ok. Synchronized!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An Error Occured: " + ex.GetBaseException().Message);
            }
        }
    }
}

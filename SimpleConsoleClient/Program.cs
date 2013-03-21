using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Controll;

namespace SimpleConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Simple Console Client for Controll");
            Console.WriteLine("https://github.com/thecopy/controll");
            Console.WriteLine();

            Console.WriteLine("Connect to localhost:10244?(Y/n)");
            var result = Console.ReadLine();
            if (string.IsNullOrEmpty(result) || result.ToLower() == "y")
            {
                Connect("http://localhost:10244/");
            }
        }

        static void _client_MessageDelivered(object sender, MessageDeliveredEventArgs e)
        {
            if (_client.Pings.Contains(e.DeliveredTicket))
            {
                Console.WriteLine("\nPong!");
            }
        }

        private static ControllClient _client;
        private static string _user = "";
        static private void Connect(string host)
        {
            Console.WriteLine("Connecting to " + host);
            _client = new ControllClient(host);
            _client.MessageDelivered += _client_MessageDelivered;
            _client.Connect();

            Console.WriteLine("Connected");
            Console.WriteLine("Type h or help for more information");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(_user + "> ");
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
                        Console.WriteLine("auth user password\t\tif no user or password is passed default will be used");
                        Console.WriteLine("list zombies\t\tlist all your zombies");
                        break;
                    case "auth":
                        if (results.Count() == 1)
                            Authenticate("username", "password");
                        else if (results.Count() == 3)
                            Authenticate(results[1], results[2]);
                        else
                            Console.WriteLine("What?");
                        break;
                    case "run":
                        string zombieName = results[1];
                        string activity = results[2];
                        string commandName = results[3];
                        string test = results.Skip(4).Aggregate((i, j) => i + " " + j);

                        string[] t = test.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var dictionary = t.ToDictionary(s => s.Split('=')[0], s => s.Split('=')[1]);
                        
                        Run(zombieName, Guid.NewGuid(), dictionary, commandName);
                        break;
                    case "list":
                        List(results[1], results.Skip(2).ToArray());
                        break;
                    case "ping":
                        Ping(results[1]);
                        break;
                    default:
                        Console.WriteLine("Unkown command " + results[0]);
                        break;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(_user + "> ");
                result = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            } while (string.IsNullOrEmpty(result) || result.ToLower() != "q" || result.ToLower() != "quit");
        }

        private static void Ping(string zombieName)
        {
            var ticket = _client.Ping(zombieName);
            Console.WriteLine("Sent ping to " + zombieName + ". Ticket: " + ticket);
        }

        private static void Run(string zombie, Guid activity, Dictionary<string,string> paramters, string commandName)
        {
            Console.WriteLine("Trying to start command " + commandName + " on activity " + activity + " on zombie " + zombie);
            Console.WriteLine("Parameters:");
            foreach (var paramter in paramters)
            {
                Console.WriteLine(paramter.Key + "=" + paramter.Value);
            }
            var ticket = _client.StartActivity(zombie, activity, paramters, commandName);

            if (ticket == Guid.Empty)
            {
                Console.WriteLine("Activation failed");
            }
            else
            {
                Console.WriteLine("OK. Ticket: " + ticket);
            }

        }

        static private void List(string what, params string[] parameters)
        {
            if (what == "zombies")
            {
                var zombies = _client.GetAllZombies().ToList();
                foreach (var zombie in zombies)
                {
                    Console.WriteLine(" * " + zombie.Name);
                }
                Console.WriteLine("Total: " + zombies.Count() + " zombies");
            }else if (what == "activities")
            {
                if (parameters.Count() != 1)
                {
                    Console.WriteLine("Syntax: list zombies <name>");
                    return;
                }

                var activities = _client.GetActivitesInstalledOnZombie(parameters[0]).ToList();
                foreach (var activity in activities)
                {
                    Console.WriteLine(" * " + activity.Name + " " + activity.Key);
                }
                Console.WriteLine("Total: " + activities.Count() + " zombies");
            }
            else
            {
                Console.WriteLine("Avaiable enumerables: zombies");
            }
        }

        static private void Authenticate(string user, string password)
        {
            Console.WriteLine("Logging in as " + user + "...");
            var result = _client.LogOn(user, password);
            if (result)
            {
                Console.WriteLine("Login Successful");
                _user = user;
            }
            else
            {
                Console.WriteLine("Login Failed");
            }
        }
    }
}

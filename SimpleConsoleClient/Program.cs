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
            Console.WriteLine("Message delivered: " + e.DeliveredTicket);
        }

        private static ControllClient _client;
        private static string _user = "";
        static private void Connect(string host)
        {
            Console.WriteLine("Connecting to " + host);
            _client = new ControllClient(host);
            _client.MessageDelivered += _client_MessageDelivered;
            _client.ActivityMessageRecieved += _client_ActivityMessageRecieved;
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
                var results = result.Split(' ').ToList();
                switch (results[0].ToLower())
                {
                    case "help":
                    case "h":
                        Console.WriteLine("* run\t\t\tRun SampleActivity on zombie names zombieName with parameters : [ {{param1} {value1}} ]");
                        Console.WriteLine("* run <zombieName> <activity-key> <commandName> <parameters>\tRun activity with id <activity-key>");
                        Console.WriteLine("* auth <[user] [password]>\t\tIf no user or password is passed: username:password will be used");
                        Console.WriteLine("* register [username] [password] <email>\tRegister user");
                        Console.WriteLine("* list [zombies|activities <zombieName>]\t\tList all your zombies or a specified zombies installed activities");
                        Console.WriteLine("* ping [zombieName]\t\tSend a ping to the zombie name <zombieName>");
                        Console.WriteLine("* status\t\t\tDispays session status");
                        break;
                    case "auth":
                        if (results.Count() == 1)
                            Authenticate("username", "password");
                        else if (results.Count() == 3)
                            Authenticate(results[1], results[2]);
                        else
                            Console.WriteLine("What?");
                        break;
                    case "register":
                        if (results.Count() == 3)
                            RegisterUser(results[1], results[2]);
                        else if (results.Count() == 4)
                            RegisterUser(results[1], results[2], results[3]);
                        else
                            Console.WriteLine("Parameter syntax error");
                        break;
                    case "run":
                        if (results.Count == 1)
                        {
                            Console.WriteLine("Activating SampleActivity on zombie names zombieName with parameters : [ {{param1} {value1}} ]");
                            Run("zombieName", Guid.Parse("1925C00C-7BD8-4D5D-BD34-78CD1D7D0EA6"), new Dictionary<string, string> { {"param1", "param2"} });
                        }
                        else if (results.Count >= 4)
                        {
                            string zombieName = results[1];
                            string activity = results[2];
                            string test = results.Skip(3).Aggregate((i, j) => i + " " + j);

                            string[] t = test.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            var dictionary = t.ToDictionary(s => s.Split('=')[0], s => s.Split('=')[1]);

                            Run(zombieName, Guid.NewGuid(), dictionary);
                        }
                        else
                        {
                            Console.WriteLine("Errornous number of parameters. Please provide at least 4. Run help for more information");
                        }
                        break;
                    case "list":
                        List(results[1], results.Skip(2).ToArray());
                        break;
                    case "ping":
                        Ping(results[1]);
                        break;
                    case "status":
                        PrintStatus();
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

        static void _client_ActivityMessageRecieved(object sender, ActivityLogMessageEventArgs e)
        {
            Console.WriteLine("Message from activity with invocation ticket " + e.Ticket + " recieved: " + e.Message);
        }

        private static void RegisterUser(string username, string password, string email = "")
        {
            var result = _client.RegisterUser(username, password, email);
            if(result)
                Console.WriteLine("Successfully registered user " + username + "!");
            else
                Console.WriteLine("Could now register user! Username or mail already in use.");
        }

        private static void PrintStatus()
        {
            Console.WriteLine("Connected to: {0}", _client.HubConnection.Url);
            Console.WriteLine("Using transport: {0}", _client.HubConnection.Transport.Name);
            Console.WriteLine("Connection Id: {0}", _client.HubConnection.ConnectionId);
            Console.WriteLine("Authenticated: {0}", string.IsNullOrEmpty(_user) ? "No" : "Yes");
            if (!string.IsNullOrEmpty(_user))
            {
                Console.WriteLine("User: {0}", _user);
            }
        }

        private static void Ping(string zombieName)
        {
            var ticket = _client.Ping(zombieName);
            Console.WriteLine("Sent ping to " + zombieName + ". Ticket: " + ticket);
        }

        private static void Run(string zombie, Guid activity, Dictionary<string,string> paramters)
        {
            Console.WriteLine("Trying to start activity " + activity + " on zombie " + zombie);
            Console.WriteLine("Parameters:");
            foreach (var paramter in paramters)
            {
                Console.WriteLine(paramter.Key + "=" + paramter.Value);
            }
            var ticket = _client.StartActivity(zombie, activity, paramters);

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
                    Console.WriteLine("Syntax: list activities <zombieName>");
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
                Console.WriteLine("Avaiable enumerables: zombies, activities");
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

using System;
using System.Collections.Generic;
using System.Linq;
using Controll.Client;
using Controll.Client.Authentication;
using Controll.Client.Models;
using Controll.Common.ViewModels;
using Newtonsoft.Json;

namespace SimpleConsoleClient
{
    class Program
    {
        private static IList<ZombieViewModel> _zombies;
        private static IDictionary<Guid, Tuple<string, Guid>> _invokedActivities; // string = zombieName, Guid = activity key
        private static IDictionary<Guid, ActivityCommandViewModel> _waitningIntermidiates;
        private static String _url;

        static void Main(string[] args)
        {
            _zombies = new List<ZombieViewModel>();
            _invokedActivities = new Dictionary<Guid, Tuple<string, Guid>>();
            _waitningIntermidiates = new Dictionary<Guid, ActivityCommandViewModel>();

            Console.WriteLine("Simple Console Client for Controll");
            Console.WriteLine("https://github.com/thecopy/controll");
            Console.WriteLine();

            Console.WriteLine("Connect to localhost:10244?(Y/n)");
            var result = Console.ReadLine();
            if (string.IsNullOrEmpty(result) || result.ToLower() == "y")
            {
                Connect("http://localhost:10244/");
            }
            else
            {
                Console.Write("Enter (http://domain:port/) url: ");
                var url = Console.ReadLine();
                Connect(url);
            }
        }

        static void _client_MessageDelivered(object sender, MessageDeliveredEventArgs e)
        {
            //Console.WriteLine("Message delivered: " + e.DeliveredTicket);
        }

        static void _client_ActivityResultRecieved(object sender, ActivityResultEventArgs e)
        {
            Console.WriteLine("Result recieved!");
            var command = JsonConvert.DeserializeObject<ActivityCommandViewModel>(e.Result.ToString());
            if (command == null) return;

            Console.WriteLine("Is it an intermidiate command! To run is type 'intermidiate'");
            _waitningIntermidiates.Add(e.Ticket, command);
        }

        private static ControllClient _client;
        static private void Connect(string host)
        {
            _url = host;
            Console.WriteLine("Connecting to " + host);

            var authenticator = new DefaultAuthenticationProvider(host);
            Console.Write("Login or register (L/r): ");
            var r = Console.ReadLine();
            if (!string.IsNullOrEmpty(r) && r.ToLower() == "r")
            {
                try
                {
                    RegisterUser();
                    Console.WriteLine("User registration successfull!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.GetBaseException().Message);
                }
            }

            Console.Write("Provide username('username'): ");
            var username = Console.ReadLine();
            if (string.IsNullOrEmpty(username)) username = "username";
            Console.Write("Provide password('password'): ");
            var password = Console.ReadLine();
            if (string.IsNullOrEmpty(password)) password = "password";

            _client = new ControllClient(_url);

            _client.MessageDelivered += _client_MessageDelivered;
            _client.ActivityMessageRecieved += _client_ActivityMessageRecieved;
            _client.ActivityResultRecieved += _client_ActivityResultRecieved;
            _client.ZombieSynchronized += ClientOnZombieSynchronized;
            _client.ZombieAdded += _client_ZombieAdded;

            _client.Connect(username, password).Wait();

            Console.WriteLine("Connected");
            Console.WriteLine("Type h or help for more information");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("> ");
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
                        Console.WriteLine("* run <zombieName>");
                        Console.WriteLine("* run <zombieName> <activity-name> <commandName>");
                        Console.WriteLine("* intermidiate");
                        Console.WriteLine("* register [username] [password] <email>\tRegister user");
                        Console.WriteLine("* list [zombies|activities <zombieName>]\t\tList all your zombies or a specified zombies installed activities");
                        Console.WriteLine("* ping [zombieName]\t\tSend a ping to the zombie name <zombieName>");
                        Console.WriteLine("* status\t\t\tDispays session status");
                        Console.WriteLine("* addzombie <zombieName>\t\t\tAdds a zombie to use currently authenticated user");
                        Console.WriteLine("* download <zombieName> <url>\t\t\tTells zombie to download activity from url");
                        break;
                    case "intermidiate":
                        if(_waitningIntermidiates.Count > 0)
                            RunIntermidiate();
                        else
                            Console.WriteLine("No waiting intermidiates!");
                        break;
                    case "run":
                        if(results.Count == 2){
                            Run(results[1]);
                        }else if (results.Count() == 4)
                        {
                            Run(results[1], results[2], results[3]);
                        }
                        else
                        {
                            Console.WriteLine("Errornous number of parameters. Please provide 1 or 3. Run help for more information");
                        }
                        break;
                    case "list":
                        List(results[1], results.Skip(2).ToArray());
                        break;
                    case "ping":
                        Ping(results[1]);
                        break;
                    case "addzombie":
                        try
                        {
                            _client.AddZombie(results[1]).Wait();
                            Console.WriteLine("Zombie '{0}' added.", results[1]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        break;
                    case "download":
                        try
                        {
                            var zombieName = results[1];
                            var url = results[2];
                            _client.DownloadActivity(zombieName, url).Wait();
                            Console.WriteLine("Zombie '{0}' requested to download activity from '{1}'", zombieName, url);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        break;
                    default:
                        Console.WriteLine("Unkown command " + results[0]);
                        break;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("> ");
                result = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            } while (string.IsNullOrEmpty(result) || result.ToLower() != "q" || result.ToLower() != "quit");
        }

        static void _client_ZombieAdded(ZombieViewModel zombieViewModel)
        {
            Console.WriteLine("Another client added a zombie: {0}", zombieViewModel.Name);
            _zombies.Add(zombieViewModel);
        }

        private static void ClientOnZombieSynchronized(string zombieName, IEnumerable<ActivityViewModel> activityViewModels)
        {
            Console.WriteLine("Zombie '{0}' synchronized it's activities.", zombieName);
            var zombie = _zombies.SingleOrDefault(x => x.Name == zombieName);

            if (zombie == null)
            {
                Console.WriteLine("(NOTE: You do not have this zombie. Refetching all zombies instead...)");
                _zombies = _client.GetAllZombies().Result.ToList();
                Console.WriteLine("OK!");
            }
            else
            {
                zombie.Activities = activityViewModels;
            }
        }

        private static void RunIntermidiate()
        {
            Console.WriteLine("Select an intermidiate:");
            for (int i = 0; i < _waitningIntermidiates.Count; i++)
            {
                Console.WriteLine(" [{0}] {1}", i, _waitningIntermidiates.ElementAt(i).Value.Label);
            }
            Console.Write("Please select an intermidiate: ");
            var enteredIndex = Console.ReadLine();
            int selectedIntermidiateIndex;
            if (!int.TryParse(enteredIndex, out selectedIntermidiateIndex) 
                || selectedIntermidiateIndex >= _waitningIntermidiates.Count
                || selectedIntermidiateIndex < 0)
            {
                Console.WriteLine("Parse error");
                return;
            }

            var selectedIntermidiate = _waitningIntermidiates.ElementAt(selectedIntermidiateIndex).Value;
            var ticket = _waitningIntermidiates.ElementAt(selectedIntermidiateIndex).Key;
            var zombieName = _invokedActivities[ticket].Item1;
            var activityKey = _invokedActivities[ticket].Item2;

            RunCommand(selectedIntermidiate, zombieName, activityKey);
            _waitningIntermidiates.Remove(ticket);
        }

        private static void Run(string zombieName)
        {
            var zombie = _zombies.SingleOrDefault(z => z.Name == zombieName);
            if (zombie == null)
            {
                Console.WriteLine("No zombie named " + zombieName + " found. If you are sure it exists please run \"list zombies\" to sync");
                return;
            }

            for (int i = 0; i < zombie.Activities.Count(); i++ )
            {
                var a = zombie.Activities.ElementAt(i);
                Console.WriteLine("[{0}] {1}", i, a.Name);
            }
            Console.Write("Please select an activity: ");
            var enteredIndex = Console.ReadLine();
            int selectedActivityIndex;
            if (!int.TryParse(enteredIndex, out selectedActivityIndex))
            {
                Console.WriteLine("Parse error");
                return;
            }

            var activity = zombie.Activities.ElementAt(selectedActivityIndex);

            for (int i = 0; i < activity.Commands.Count(); i++)
            {
                var c = activity.Commands.ElementAt(i);
                Console.WriteLine("[{0}] {1}", i, c.Name);
            }
            Console.WriteLine("Please select a command: ");
            enteredIndex = Console.ReadLine();
            int selectedCommandIndex;
            if (!int.TryParse(enteredIndex, out selectedCommandIndex))
            {
                Console.WriteLine("Parse error");
                return;
            }

            Run(zombieName, activity.Name, activity.Commands.ElementAt(selectedCommandIndex).Name);
        }

        private static void Run(string zombieName, string activityName, string commandName)
        {
            var zombie = _zombies.SingleOrDefault(z => z.Name == zombieName);
            if (zombie == null)
            {
                Console.WriteLine("No zombie named " + zombieName + " found. If you are sure it exists please run \"list zombies\" to sync");
                return;
            }
            var activity = zombie.Activities.SingleOrDefault(a => a.Name == activityName);
            if (activity == null)
            {
                Console.WriteLine("No activity named " + activityName + " found. If you are sure it exists please run \"list zombies\" to sync");
                return;
            }
            var command = activity.Commands.SingleOrDefault(c => c.Name == commandName);
            if (command == null)
            {
                Console.WriteLine("No command named " + commandName + " found. If you are sure it exists please run \"list zombies\" to sync");
                return;
            }

            RunCommand(command, zombieName, activity.Key);
        }

        private static void RunCommand(ActivityCommandViewModel command, string zombieName, Guid activityKey)
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Running command " + command.Label);
            Console.WriteLine();
            var parameters = new Dictionary<string, string>();
            
            foreach (var parameter in command.ParameterDescriptors)
            {
                Console.WriteLine(parameter.Label + " [" + parameter.Description + "]");
                if (parameter.PickerValues == null || !parameter.PickerValues.Any())
                {
                    Console.Write(": ");
                    var value = Console.ReadLine();
                    parameters.Add(parameter.Name, value);
                }
                else
                {
                    for (int pv = 0; pv < parameter.PickerValues.Count(); pv++)
                    {
                        var v = parameter.PickerValues.ElementAt(pv);
                        Console.WriteLine(" [{0}] {1} ({2})", pv, v.Label, v.Description);
                    }
                    Console.Write(": ");
                    var enteredIndex = Console.ReadLine();
                    int selectedPickerIndex;
                    if (!int.TryParse(enteredIndex, out selectedPickerIndex) || selectedPickerIndex >= parameter.PickerValues.Count())
                    {
                        Console.WriteLine("Parse error");
                        return;
                    }
                    parameters.Add(parameter.Name, parameter.PickerValues.ElementAt(selectedPickerIndex).Identifier);
                }
            }

            Console.WriteLine("OK. Sending invocation message...");
            var ticket = _client.StartActivity(zombieName, activityKey, parameters, command.Name).Result;
            if (ticket.Equals(Guid.Empty))
            {
                Console.WriteLine("Unkown error sending invocation message!");
                return;
            }
            _invokedActivities.Add(ticket, new Tuple<string, Guid>(zombieName, activityKey));
            Console.WriteLine("OK!");
        }

        static void _client_ActivityMessageRecieved(object sender, ActivityLogMessageEventArgs e)
        {
            Console.WriteLine("Message recieved: " + e.Message);
        }

        private static void RegisterUser()
        {
            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();
            Console.Write("Email: ");
            var email = Console.ReadLine();

            var authenticator = new DefaultAuthenticationProvider(_url);

            authenticator.RegisterUser(username, password, email).Wait();
        }

        private static void Ping(string zombieName)
        {
            var ticket = _client.Ping(zombieName);
            Console.WriteLine("Sent ping to " + zombieName + ". Ticket: " + ticket);
        }

        static private void List(string what, params string[] parameters)
        {
            if (what == "zombies")
            {
                var zombies = _client.GetAllZombies().Result.ToList();
                foreach (var zombie in zombies)
                {
                    Console.WriteLine(" * " + zombie.Name);
                }
                _zombies = zombies;
                Console.WriteLine("Total: " + zombies.Count() + " zombies");
            }else if (what == "intermidiates")
            {
                Console.WriteLine("Avaiable intermidiates: ");
                foreach (var pair in _waitningIntermidiates)
                {
                    Console.WriteLine(" * " + pair.Value.Name);
                }
                Console.WriteLine("Total: " + _waitningIntermidiates.Count() + " intermidiates");
            }
            else
            {
                Console.WriteLine("Avaiable enumerables: zombies, activities, intermidiates");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll;

namespace SimpleConsoleZombie
{
    class Program
    {
        private static ZombieService service;
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
        }

        private static void Connect(string url)
        {
            Console.WriteLine("Initializing service...");
            service = new ZombieService("username", "zombieName");
            Console.WriteLine("Connecting and logging on to " + url);
            var startResult = service.Start("password");
            if (!startResult)
            {
                Console.WriteLine("Could not connect and/or login to user \"username\" as zombie \"zombieName\"");
                return;
            }

            Console.WriteLine("Connected!");
            Console.WriteLine("Type h or help for more information");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(service.ZombieName + "@" + service.UserName + "> ");
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
                        break;
                    case "status":
                        Console.WriteLine("Not implemented");
                        break;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(service.ZombieName + "@" + service.UserName + "> ");
                result = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            } while (string.IsNullOrEmpty(result) || result.ToLower() != "q" || result.ToLower() != "quit");
        }
        
    }
}

﻿using System;
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
            service = new ZombieService("http://localhost:10244/");
            Console.WriteLine("Connecting to " + url);
            service.Connect();
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
                    case "auth":
                        Authenticate();
                        break;
                    case "register":
                        if (results.Count() == 4)
                        {
                            Register(results[1], results[2], results[3]);
                        }
                        break;
                    case "status":
                        PrintStatus();
                        break;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(service.ZombieName + "@" + service.UserName + "> ");
                result = Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            } while (string.IsNullOrEmpty(result) || result.ToLower() != "q" || result.ToLower() != "quit");
        }

        private static void PrintStatus()
        {
            Console.WriteLine("Connected to: {0}", service.HubConnection.Url);
            Console.WriteLine("Transport: {0}", service.HubConnection.Transport.Name);
            Console.WriteLine("Connection-Id: {0}", service.HubConnection.ConnectionId);
            Console.WriteLine("Authenticated: {0}", string.IsNullOrEmpty(service.UserName) ? "No" : "Yes");
            if (!string.IsNullOrEmpty(service.UserName))
            {
                Console.WriteLine("Logged in on user: " + service.UserName);
                Console.WriteLine("Logged in as zombie: " + service.ZombieName);
            }
        }

        private static void Register(string userName, string password, string zombieName)
        {
            bool result = service.Register(userName, password, zombieName);
            if (result)
            {
                Console.WriteLine("Registration was successfull! Auth as this zombie(Y/n)?");
                var what = Console.ReadLine();
                if (string.IsNullOrEmpty(what) || what.ToLower() == "y")
                {
                    Authenticate(userName, password, zombieName);
                }
            }
            else
            {
                Console.WriteLine("Registration failed. Wrong username, password or zombie name already exists");
            }
        }
        private static void Authenticate(string userName, string password, string zombieName)
        {
            Console.WriteLine("Authenticating...");
            if (string.IsNullOrEmpty(userName))
                userName = "username";
            if (string.IsNullOrEmpty(password))
                password = "password";
            if (string.IsNullOrEmpty(zombieName))
                zombieName = "zombieName";
            var loginResult = service.Authenticate(userName, password, zombieName);

            Console.WriteLine(!loginResult ? "Failed! Please try again." : "Authentication was successfull.");
        }

        private static void Authenticate()
        {
            Console.Write("Enter username (username): ");
            var userName = Console.ReadLine();
            Console.Write("Enter password (password): ");
            var password = Console.ReadLine();
            Console.Write("Enter zombie name (zombieName): ");
            var zombieName = Console.ReadLine();

            Authenticate(userName, password, zombieName);
        }
        
    }
}

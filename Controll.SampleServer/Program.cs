using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting;
<<<<<<< HEAD
using Controll.Hosting.NHibernate;
=======
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1

namespace Controll.SampleServer
{
    class Program
    {
        private static ControllServer _server;
        static void Main(string[] args)
        {
            _server = new ControllServer("http://*:10244/");

            Console.WriteLine("Starting server at http://*:10244");

            _server.Start();

            Console.WriteLine("OK - Server started");

<<<<<<< HEAD
            string line;
            while ((line = Console.ReadLine()) != "q")
            {
                Console.WriteLine("Command " + line + " not recognized");
=======
            while (Console.ReadLine() != "q")
            {
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
            }
        }
    }
}

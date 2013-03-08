using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting;
using Controll.Hosting.NHibernate;

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

            string line;
            while ((line = Console.ReadLine()) != "q")
            {
                Console.WriteLine("Command " + line + " not recognized");
            }
        }
    }
}

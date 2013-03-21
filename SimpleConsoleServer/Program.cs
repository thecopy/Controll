using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting;

namespace SimpleConsoleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Simple Console Server for Controll");
            Console.WriteLine("https://github.com/thecopy/controll");
            
            const string url = "http://*:10244/";
            var server = new ControllServer(url);

            Console.WriteLine("Starting server on " + url);

            using (server.Start())
            {
                Console.WriteLine("Listening...");
                while (true)
                    Console.WriteLine(Console.ReadLine());
            }
        }
    }
}

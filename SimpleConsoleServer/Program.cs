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
            var server = new ControllStandAloneServer(url);

            Console.Write("Use NHibernate Profiler? Enter y or Y, else just type anything: ");
            var read = Console.ReadLine();
            if (read != null && read.ToLower() == "y")
            {
                Console.WriteLine("Ok. Will use NHibernate Profiler");
                HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();
            }

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

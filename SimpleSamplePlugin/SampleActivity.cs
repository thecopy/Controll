using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Controll.Common;

namespace SimpleSamplePlugin
{
    [ActivityAttribute("1925C00C-7BD8-4D5D-BD34-78CD1D7D0EA6")]
    public class SampleActivity : IControllPlugin
    {
        public Guid Key { get { return Guid.Parse("1925C00C-7BD8-4D5D-BD34-78CD1D7D0EA6"); } }

        public string Name { get { return "Sample Activity"; } }
        public string CreatorName { get { return "Creator Name"; } }
        public DateTime LastUpdated { get { return DateTime.Parse("2013-01-02"); } }
        public string Description { get { return "Sample Activity which does nothing (besides sleeping for 5 seconds)."; } }

        public void Execute(IPluginContext context)
        {
            Console.WriteLine("SampleActivity executed!");
            Console.WriteLine("Parameters :");
            foreach(var param in context.Parameters)
                Console.WriteLine("{0} = {1}", param.Key, param.Value);

            Console.WriteLine("Calling Started()");
            context.Started();

            Console.WriteLine("Sleeping 5 seconds...");
            Thread.Sleep(5000);

            Console.WriteLine("Calling Finished(string)");
            context.Finish("Finished:)");
        }
    }
}

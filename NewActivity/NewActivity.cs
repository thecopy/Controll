using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Controll.Common;

namespace NewActivity
{
    [ActivityAttribute("27611FAD-17CD-463B-A179-796F3E3B1121")]
    public class NewActivity : IPlugin
    {
        public Guid Key { get; private set; }
        public string Name { get; private set; }
        public void Execute(IPluginContext context)
        {
            context.Started();

            Thread.Sleep(5000);

            context.Notify("Här händer det saker ja du! Sjutton järnspikar! ");

            Thread.Sleep(5000);

            context.Finish("DET GICK BRA! RESULTAT: 103 kB ??");
        }
    }
}

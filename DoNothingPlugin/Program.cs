using System;
using System.Threading;
using System.Windows.Forms;
using Controll;
using Controll.Common;

namespace DoNothingPlugin
{
    public class DoNothingPlugin : IPlugin
    {
        public Guid Key { get { return Guid.Parse("2765BFAD-17CD-463B-A179-796F3E3B1120"); } }

        public string Name { get { return "Do-Nothing Controll Plugin"; } }
        public void Execute(IPluginContext context)
        {
            // Do nothing

            MessageBox.Show("Ja! Parameter: " + context.Parameters);

            context.Finish("ok");
        }
    }
}

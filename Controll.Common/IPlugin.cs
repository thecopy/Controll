using System;

namespace Controll.Common
{
    public interface IPlugin
    {
        Guid Key { get; }
        string Name { get; }
        void Execute(IPluginContext context);
    }
}

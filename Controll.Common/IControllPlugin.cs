using System;

namespace Controll.Common
{
    public interface IControllPlugin
    {
        void Execute(IPluginContext context);

        Guid Key { get; }
        string Name { get; }
        string CreatorName { get; }
        DateTime LastUpdated { get; }
        string Description { get; }
    }
}

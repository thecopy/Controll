using System;
using System.Collections.Generic;
using Controll.Common.ViewModels;

namespace Controll.Common
{
    public interface IActivity
    {
        void Execute(IActivityContext context);

        ActivityViewModel ViewModel { get; }
    }
}

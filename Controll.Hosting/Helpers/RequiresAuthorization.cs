﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.Hosting.Helpers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequiresAuthorization : Attribute
    {}
}

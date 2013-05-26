using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin.Types;

namespace Controll.Hosting.Helpers
{
    public static class RequestHelper
    {
        public static String GetBodyRequestPart(string body, string partName)
        {
            var parts = body.Split('&');
            
            foreach (var part in parts)
            {
                if (part.StartsWith(partName))
                    return part.Substring(partName.Length + 1);
            }

            return null;
        }
    }
}

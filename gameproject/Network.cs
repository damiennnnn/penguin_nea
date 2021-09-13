using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gameproject
{
    public static class Networking
    {

        public static void Connect(string IP = "")
        {
            if (String.IsNullOrEmpty(IP))
            {
                Main.Console.Output.Append("No IP specified");
                return;
            }

            Main.Console.Output.Append(string.Format("Connecting to {0}...", IP));

        }

    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using QuakeConsole;
using System.Linq;
using System.Collections.Generic;
using System;

namespace gameproject
{

    public class CustomInterpreter : ICommandInterpreter
    {
        Dictionary<string, Action> Commands = new Dictionary<string, Action>();

        public void AddCommand(string command, Action action)
        {
            if (!string.IsNullOrEmpty(command)) // add command to the commands list if a valid string is passed 
                Commands.Add(command, action);
        }


        public void Autocomplete(IConsoleInput input, bool forward)
        {
            throw new NotImplementedException(); // not implemented, required to fulfil the interface
        }

        public void Execute(IConsoleOutput output, string command)
        {
            Main.Console.Output.Append(command); // echo command back to console 

            List<string> param = command.Split(' ').ToList(); // split command with the space character into the comand and its parameters (if any)
            string cmd = param[0]; // first string of the list should be the command issued
            param.RemoveAt(0); // we can remove the command after storing it seperately
            Action act;
            Variable var;

            if (Commands.TryGetValue(cmd, out act)) // try to determine if the command exists before trying to execute it, prevents crashing
                act();

            else if (Global.ConsoleVars.TryGetValue(cmd, out var)){
                int result = 0;
                bool parsed = false;
                if (param.Count > 0)
                    parsed = int.TryParse(param[0], out result);

                if (parsed)
                    var.Update(result);
                else
                    Main.Console.Output.Append(string.Format("Description: {0} Value: {1}", var.desc, var.val));
            }

        }

        
    }
}

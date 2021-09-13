using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using QuakeConsole;
using System.Linq;
using System.Collections.Generic;
using System;

namespace gameproject
{
    public class Variable
    {
        public string identifier;
        public Type type;
        public object val;
        public string desc = string.Empty;
        public Action onChange; // this action will be called when specific variables are modified by the user
        public Variable(string id, object obj, Type t)
        {
            identifier = id;
            val = obj;
            type = t;
        }
        public void Update(object newVal)
        {
            val = newVal;
            onChange();
        }
    }

    public static class Global
    {
        public static Dictionary<string, Variable> ConsoleVars = new Dictionary<string, Variable>();

        public static void RegisterVariable(string id, object obj, string desc = "")
        {
            var variable = new Variable(id, obj, obj.GetType());
            if (!string.IsNullOrEmpty(desc))
                variable.desc = desc;

            ConsoleVars.Add(id, variable); // bind variable to modify to the new consolevar
            ConsoleVars[id].onChange = new Action(() => { }); // do nothing
        }
        public static void RegisterVariable(string id, object val, Action update)
        {
            RegisterVariable(id, val); // use original method 
            ConsoleVars[id].onChange = update; // action to invoke when the variable is changed
        }
    }

}

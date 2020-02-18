using System;
using System.Reflection;
using AngryWasp.Helpers;
using AngryWasp.Serializer;

namespace EMS.Commands.CLI
{
    public class SetConfig
    {
        public static bool Handle(string[] cmd)
        {
            CommandLineParser clp = CommandLineParser.Parse(cmd);
            if (clp.Count != 3)
            {
                Log.WriteError("Incorrect number of arguments");
                return false;
            }

            string propName = clp[1].Value;
            string propValue = clp[2].Value;

            var props = ReflectionHelper.Instance.GetProperties(typeof(UserConfig), Property_Access_Mode.Read | Property_Access_Mode.Write);
            PropertyInfo pi = null;

            foreach (var prop in props)
            {
                if (prop.Key == propName)
                {
                    pi = prop.Value;
                    break;
                }
            }

            if (pi == null)
            {
                Log.WriteError($"Property {propName} is invalid");
                return false;
            }

            object value = Serializer.Deserialize(pi.PropertyType, propValue);
            pi.SetValue(Config.User, value);

            return true;
        }
    }
}
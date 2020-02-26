using System.Reflection;
using AngryWasp.Helpers;
using AngryWasp.Serializer;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("set", "Set a config option value. Usage: set <param> <value>")]
    public class SetConfig : IApplicationCommand
    {
        public bool Handle(string command)
        {
            string propName = Helpers.PopWord(ref command);
            string propValue = Helpers.PopWord(ref command);

            if (string.IsNullOrEmpty(propName) || string.IsNullOrEmpty(propValue))
            {
                Log.WriteError("Incorrect number of arguments");
                return false;
            }
            
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

            Config.Save();

            // All other config options require a restart
            if (propName != "MessageExpiration" && propName != "KeyFile" && propName != "RelayOnly")
                Log.WriteWarning("A restart is required for the changes to take effect");

            if (Config.User.RelayOnly)
                KeyRing.EraseKey();
            else
                KeyRing.ReadKey();

            return true;
        }
    }
}
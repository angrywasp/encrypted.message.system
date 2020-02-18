using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class NewAddress
    {
        public static bool Handle(string[] cmd)
        {
            CommandLineParser clp = CommandLineParser.Parse(cmd);
            string file = null;
            if (clp.Count >= 2)
                file = clp[1].Value;

            KeyRing.Initialize(file);
            return true;
        }
    }
}
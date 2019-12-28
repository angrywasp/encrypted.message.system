using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class MarkMessageRead
    {
        public static bool Handle(string[] cmd)
        {
            CommandLineParser clp = CommandLineParser.Parse(cmd);
            if (clp.Count != 2)
            {
                Log.WriteError("Incorrect number of arguments");
                return false;
            }

            HashKey16 key = clp[1].Value.FromByteHex();
            return MessagePool.MarkMessageRead(key);
        }
    }
}

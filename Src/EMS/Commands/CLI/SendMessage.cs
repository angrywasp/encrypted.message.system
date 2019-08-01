using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class SendMessage
    {
        public static bool Handle(string[] cmd)
        {
            CommandLineParser clp = CommandLineParser.Parse(cmd);
            HashKey hk = MessagePool.Send(clp[1].Value, clp[2].Value);
            return (hk != HashKey.Empty);
        }
    }
}
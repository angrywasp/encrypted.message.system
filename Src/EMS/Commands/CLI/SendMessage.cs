using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class SendMessage
    {
        public static bool Handle(string[] cmd)
        {
            CommandLineParser clp = CommandLineParser.Parse(cmd);
            if (clp.Count != 3 && clp.Count != 4)
            {
                Log.WriteError("Incorrect number of arguments");
                return false;
            }

            uint expiration = 3600; //default expiration period of 1 hour, 3600 seconds

            if (clp.Count == 4)
                uint.TryParse(clp[3].Value, out expiration);

            HashKey16 key;
            bool sent = MessagePool.Send(clp[1].Value, clp[2].Value, expiration, out key);

            if (sent)
                Log.WriteConsole($"Sent message with key {key}");
            else
                Log.WriteError($"Failed to send message");

            return sent;
        }
    }
}
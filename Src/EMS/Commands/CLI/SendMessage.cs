using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class SendMessage
    {
        public static bool Handle(string[] cmd)
        {
            CommandLineParser clp = CommandLineParser.Parse(cmd);
            if (clp.Count != 3)
            {
                Log.WriteError("Incorrect number of arguments");
                return false;
            }

            HashKey16 key;
            bool sent = MessagePool.Send(clp[1].Value, clp[2].Value, out key);

            if (sent)
                Log.WriteConsole($"Sent message with key {key}");
            else
                Log.WriteError($"Failed to send message");

            return sent;
        }
    }
}
using System.Text;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class SendMessage
    {
        public static bool Handle(string command)
        {
            string address = Helpers.PopWord(ref command);

            HashKey16 key;
            bool sent = MessagePool.Send(address, command, Config.User.MessageExpiration, out key);

            if (sent)
                Log.WriteConsole($"Sent message with key {key}");
            else
                Log.WriteError($"Failed to send message");

            return sent;
        }
    }
}
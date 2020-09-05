using AngryWasp.Cli;
using System.Text;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("send", "Send a message. Usage: send <address> <message>")]
    public class SendMessage : IApplicationCommand
    {
        public bool Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }
            
            string address = Helpers.PopWord(ref command);

            HashKey16 key;
            bool sent = MessagePool.Send(address, Message_Type.Text, Encoding.ASCII.GetBytes(command), Config.User.MessageExpiration, out key);

            if (sent)
                Log.WriteConsole($"Sent message with key {key}");
            else
                Log.WriteError($"Failed to send message");

            return sent;
        }
    }
}
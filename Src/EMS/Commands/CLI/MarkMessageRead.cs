using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("flag", "Mark a message as read. Usage: flag <message_hash>")]
    public class MarkMessageRead : IApplicationCommand
    {
        public bool Handle(string command)
        {
            string hex = Helpers.PopWord(ref command);

            if (string.IsNullOrEmpty(hex) || hex.Length != 32)
            {
                Log.WriteError("Incorrect number of arguments");
                return false;
            }

            if (hex.Length != 32)
            {
                Log.WriteError("Invalid argument");
                return false;
            }

            HashKey16 key = hex.FromByteHex();
            return MessagePool.MarkMessageRead(key);
        }
    }
}

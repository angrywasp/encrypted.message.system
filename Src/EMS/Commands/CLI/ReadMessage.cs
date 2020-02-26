using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("read", "Read a message. Usage: read <message_hash>")]
    public class ReadMessage : IApplicationCommand
    {
        public bool Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }
            

            string hex = Helpers.PopWord(ref command);

            if (string.IsNullOrEmpty(hex))
                hex = MessagePool.LastReceivedMessage != null ? MessagePool.LastReceivedMessage.Key.ToString() : null;

            if (string.IsNullOrEmpty(hex))
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

            Message message;

            if (!MessagePool.Messages.TryGetValue(key, out message))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Message not found.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            if (!message.IsDecrypted)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Message is encrypted. Cannot read.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            string dir = message.Direction.ToString().PadLeft(6);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"{dir}: {message.Address}");
            Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(message.Timestamp)}");
            Console.WriteLine($"  Type: {message.MessageType} (Version {message.MessageVersion})");
            Console.WriteLine($"Expiry: {DateTimeHelper.UnixTimestampToDateTime(message.Timestamp + message.Expiration)}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine(message.ParseDecryptedData());
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            return true;
        }
    }
}
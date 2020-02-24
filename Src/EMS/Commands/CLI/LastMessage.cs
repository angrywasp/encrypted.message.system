using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class LastMessage
    {
        public static bool Handle(string command)
        {
            Message message = MessagePool.LastReceivedMessage;

            if (message == null)
            {
                Log.WriteError("No messages received since starting the node.");
                return false;
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"  From: {message.Address}");
            Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(message.Timestamp)}");
            Console.WriteLine($"Expiry: {DateTimeHelper.UnixTimestampToDateTime(message.Timestamp + message.Expiration)}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message.DecryptedMessage);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;

            return true;
        }
    }
}
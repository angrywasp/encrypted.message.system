using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class ReadMessage
    {
        public static bool Handle(string[] cmd)
        {
            HashKey key = cmd[1].FromByteHex();

            if (!MessagePool.DecryptedMessages.ContainsKey(key))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Message not found");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            var data = MessagePool.DecryptedMessages[key];

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"From: {data.Sender}");
            Console.WriteLine($"Time: {DateTimeHelper.UnixTimestampToDateTime(data.TimeStamp)}");
            Console.WriteLine(data.Message);
            Console.ForegroundColor = ConsoleColor.White;

            return true;
        }
    }
}
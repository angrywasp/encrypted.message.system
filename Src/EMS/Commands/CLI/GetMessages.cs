using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class GetMessages
    {
        public static bool Handle(string[] cmd)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine("Encrypted:");

            Console.ForegroundColor = ConsoleColor.Green;

            int count = 0;
            foreach (var m in MessagePool.Messages)
            {
                Console.WriteLine($"  Hash: {m.Key}");
                Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(m.Value.TimeStamp)}");
                Console.WriteLine();

                ++count;
            }

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("None");
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine("Decrypted:");

            Console.ForegroundColor = ConsoleColor.Green;

            count = 0;
            foreach (var m in MessagePool.DecryptedMessages)
            {
                Console.WriteLine($"  Hash: {m.Key}");
                Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(m.Value.TimeStamp)}");
                Console.WriteLine($"Sender: {m.Value.Sender}");
                Console.WriteLine();
                ++count;
            }

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("None");
            }

            Console.ForegroundColor = ConsoleColor.White;

            return true;
        }
    }
}
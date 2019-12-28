using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class GetMessages
    {
        public static bool Handle(string[] cmd)
        {
            
#region Encrypted

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Encrypted:");
            Console.ForegroundColor = ConsoleColor.Green;

            int count = 0;
            foreach (var m in MessagePool.EncryptedMessages)
            {
                if (m.Value.HasReadProof())
                    Console.WriteLine($"  Read: {m.Key}");
                else
                    Console.WriteLine($"Unread: {m.Key}");
                ++count;
            }

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  None");
            }

#endregion

            Console.WriteLine();

#region Incoming

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Incoming:");
            Console.ForegroundColor = ConsoleColor.Green;

            count = 0;
            foreach (var m in MessagePool.IncomingMessages)
            {
                if (m.Value.IsRead)
                    Console.WriteLine($"  Read: {m.Key}");
                else
                    Console.WriteLine($"Unread: {m.Key}");
                Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(m.Value.TimeStamp)}");
                Console.WriteLine($"  From: {m.Value.Sender}");
                ++count;
            }

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  None");
            }

#endregion

            Console.WriteLine();

#region Outgoing

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Outgoing:");
            Console.ForegroundColor = ConsoleColor.Green;

            count = 0;
            foreach (var m in MessagePool.OutgoingMessages)
            {
                Console.WriteLine($"  Hash: {m.Key}");
                Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(m.Value.TimeStamp)}");
                Console.WriteLine($"    To: {m.Value.Recipient}");
                ++count;
            }

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  None");
            }

#endregion

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;

            return true;
        }
    }
}
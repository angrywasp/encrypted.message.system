using System;
using System.Collections.Generic;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class GetMessages
    {
        public static bool Handle(string[] cmd)
        {

            List<Message> encrypted = new List<Message>();
            List<Message> decrypted = new List<Message>();

            foreach (var m in MessagePool.Messages)
            {
                if (m.Value.IsDecrypted)
                    decrypted.Add(m.Value);
                else
                    encrypted.Add(m.Value);
            }
            
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Encrypted:");
            Console.ForegroundColor = ConsoleColor.Green;

            if (DisplayList(encrypted) == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  None");
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Decrypted:");
            Console.ForegroundColor = ConsoleColor.Green;

            if (DisplayList(decrypted) == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  None");
                Console.WriteLine();
            }
            
            Console.ForegroundColor = ConsoleColor.White;

            return true;
        }

        private static int DisplayList(List<Message> messages)
        {
            int count = 0;

            foreach (var m in messages)
            {
                if (m.ReadProof != null && m.ReadProof.IsRead)
                    Console.WriteLine($"  Read: {m.Key}");
                else
                    Console.WriteLine($"Unread: {m.Key}");
                    
                Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(m.Timestamp)}");
                Console.WriteLine($"Expiry: {DateTimeHelper.UnixTimestampToDateTime(m.Timestamp + m.Expiration)}");

                if (m.IsDecrypted)
                {
                    bool incoming = true;
                    if (MessagePool.OutgoingMessages.Contains(m.Key))
                        incoming = false;

                    Console.WriteLine($"{(incoming ? "  From:" : "    To:")} {m.Address}");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    if (incoming)
                        Console.WriteLine("        Incoming message");
                    else
                        Console.WriteLine("        Outgoing message");
                    Console.ForegroundColor = ConsoleColor.Green;
                }

                Console.WriteLine();
                
                ++count;
            }

            return count;
        }
    }
}
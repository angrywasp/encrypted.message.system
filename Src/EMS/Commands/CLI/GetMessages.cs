using System;
using System.Collections.Generic;
using AngryWasp.Helpers;
using System.Linq;

namespace EMS.Commands.CLI
{
    public class GetMessages
    {
        public static bool Handle(string command)
        {
            List<Message> encrypted = new List<Message>();
            List<Message> incoming = new List<Message>();
            List<Message> outgoing = new List<Message>();

            foreach (var m in MessagePool.Messages)
            {
                if (m.Value.IsDecrypted)
                {
                    if (MessagePool.OutgoingMessages.Contains(m.Key))
                        outgoing.Add(m.Value);
                    else
                        incoming.Add(m.Value);
                }
                else
                    encrypted.Add(m.Value);
            }

            encrypted = encrypted.OrderBy(x => x.Timestamp).ToList();
            incoming = incoming.OrderBy(x => x.Timestamp).ToList();
            outgoing = outgoing.OrderBy(x => x.Timestamp).ToList();
            
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
            Console.WriteLine("Outgoing:");
            Console.ForegroundColor = ConsoleColor.Green;

            if (DisplayList(outgoing) == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  None");
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Incoming:");
            Console.ForegroundColor = ConsoleColor.Green;

            if (DisplayList(incoming) == 0)
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
                }

                Console.WriteLine();
                
                ++count;
            }

            return count;
        }
    }
}
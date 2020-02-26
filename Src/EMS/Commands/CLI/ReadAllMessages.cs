using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("read_all", "Read all unread messages")]
    public class ReadAllMessages : IApplicationCommand
    {
        public bool Handle(string command)
        {
            foreach (var m in MessagePool.Messages.Values)
            {
                if (m.IsDecrypted && 
                    m.Direction == Message_Direction.In &&
                    !m.ReadProof.IsRead)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"  From: {m.Address}");
                    Console.WriteLine($"  Time: {DateTimeHelper.UnixTimestampToDateTime(m.Timestamp)}");
                    Console.WriteLine($"  Type: {m.MessageType} (Version {m.MessageVersion})");
                    Console.WriteLine($"Expiry: {DateTimeHelper.UnixTimestampToDateTime(m.Timestamp + m.Expiration)}");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine();
                    Console.WriteLine(m.ParseDecryptedData());
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            return true;
        }
    }
}
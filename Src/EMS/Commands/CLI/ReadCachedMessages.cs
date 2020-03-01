using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("read_cached", "Read all cached messages")]
    public class ReadCachedMessages : IApplicationCommand
    {
        public bool Handle(string command)
        {
            foreach (var cm in MessagePool.MessageCache)
            {
                Message m = cm.Validate(false);
                if (m.IsDecrypted)
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
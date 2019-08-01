using System;
using AngryWasp.Net;

namespace EMS.Commands.CLI
{
    public class GetPeers
    {
        public static bool Handle(string[] cmd)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine("Incoming:");

            Console.ForegroundColor = ConsoleColor.Green;

            int count = 0;
            ConnectionManager.ForEach(Direction.Incoming, (c) =>
            {
                Console.WriteLine($"{c.PeerId} - {c.Address}:{c.Port}");
                ++count;
            });

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("None");
            }

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine("Outgoing:");

            Console.ForegroundColor = ConsoleColor.Green;

            count = 0;
            ConnectionManager.ForEach(Direction.Outgoing, (c) =>
            {
                Console.WriteLine($"{c.PeerId} - {c.Address}:{c.Port}");
                ++count;
            });

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
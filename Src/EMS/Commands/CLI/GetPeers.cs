using System;
using System.Collections.Generic;
using AngryWasp.Net;

namespace EMS.Commands.CLI
{
    public class GetPeers
    {
        public static bool Handle(string command)
        {
            
#region Incoming

            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine("Incoming:");

            Console.ForegroundColor = ConsoleColor.Green;

            int count = 0;
            ConnectionManager.ForEach(Direction.Incoming, (c) =>
            {
                Console.WriteLine($"{c.PeerId} - {c.Address.MapToIPv4()}:{c.Port}");
                ++count;
            });

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("None");
            }

#endregion

            Console.WriteLine();

#region Outgoing

            Console.ForegroundColor = ConsoleColor.DarkGreen;

            Console.WriteLine("Outgoing:");

            Console.ForegroundColor = ConsoleColor.Green;

            count = 0;
            List<Connection> disconnected = new List<Connection>();
            ConnectionManager.ForEach(Direction.Outgoing, (c) =>
            {
                try {
                    Console.WriteLine($"{c.PeerId} - {c.Address.MapToIPv4()}:{c.Port}");
                    ++count;
                } catch {
                    disconnected.Add(c);
                }
                
            });

            foreach (var d in disconnected)
                ConnectionManager.Remove(d);

            if (count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("None");
            }

#endregion

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;

            return true;
        }
    }
}
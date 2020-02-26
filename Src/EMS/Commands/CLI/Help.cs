using System;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("help", "Print this help text")]
    public class Help : IApplicationCommand
    {
        public bool Handle(string command)
        {
            Console.WriteLine();
            foreach (var cmd in Application.Commands)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(cmd.Key);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($": {cmd.Value.Item1}");
            }
            return true;
        }
    }
}
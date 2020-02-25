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
                Console.WriteLine($"{cmd.Key}: {cmd.Value.Item1}");
            return true;
        }
    }
}
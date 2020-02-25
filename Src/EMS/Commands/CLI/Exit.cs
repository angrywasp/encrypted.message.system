using System;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("exit", "Exit the program")]
    public class Exit : IApplicationCommand
    {
        public bool Handle(string command)
        {
            Environment.Exit(0);
            return true;
        }
    }
}
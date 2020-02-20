using System;

namespace EMS.Commands.CLI
{
    public class Exit
    {
        public static bool Handle(string command)
        {
            Environment.Exit(0);
            return true;
        }
    }
}
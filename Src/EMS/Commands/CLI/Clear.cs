using System;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("clear", "Clears the console")]
    public class Clear : IApplicationCommand
    {
        public bool Handle(string command)
        {
            Console.Clear();
            if (Environment.GetEnvironmentVariable("TERM").StartsWith("xterm")) 
                Console.WriteLine("\x1b[3J");
            Console.CursorTop = 0;
            Log.WriteConsole($"EMS {Version.VERSION}: {Version.CODE_NAME}");
            return true;
        }
    }
}
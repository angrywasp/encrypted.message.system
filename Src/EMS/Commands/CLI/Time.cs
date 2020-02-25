using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("time", "Display the current UTC time")]
    public class Time : IApplicationCommand
    {
        public bool Handle(string command)
        {
            Log.WriteConsole($"Current UTC time: {DateTime.UtcNow} ({DateTimeHelper.TimestampNow})");
            return true;
        }
    }
}
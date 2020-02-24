using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class Time
    {
        public static bool Handle(string command)
        {
            Log.WriteConsole($"Current UTC time: {DateTime.UtcNow} ({DateTimeHelper.TimestampNow})");
            return true;
        }
    }
}
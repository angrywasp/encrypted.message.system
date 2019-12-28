using System;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class ReadMessage
    {
        public static bool Handle(string[] cmd)
        {
            CommandLineParser clp = CommandLineParser.Parse(cmd);
            if (clp.Count != 2)
            {
                Log.WriteError("Incorrect number of arguments");
                return false;
            }

            HashKey16 key = clp[1].Value.FromByteHex();

            IncomingMessage incomingMessage;
            OutgoingMessage outgoingMessage;

            if (MessagePool.IncomingMessages.TryGetValue(key, out incomingMessage))
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"From: {incomingMessage.Sender}");
                Console.WriteLine($"Time: {DateTimeHelper.UnixTimestampToDateTime(incomingMessage.TimeStamp)}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(incomingMessage.Message);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                return true;
            }

            if (MessagePool.OutgoingMessages.TryGetValue(key, out outgoingMessage))
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"  To: {outgoingMessage.Recipient}");
                Console.WriteLine($"Time: {DateTimeHelper.UnixTimestampToDateTime(outgoingMessage.TimeStamp)}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(outgoingMessage.Message);
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                return true;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Message not found");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            return false;
        }
    }
}
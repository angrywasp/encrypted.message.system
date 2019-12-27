using System;
using System.Collections.Generic;

namespace AngryWasp.Logger
{
    public class BufferedLogWriter : ILogWriter
    {
        Queue<Tuple<ConsoleColor, string>> buffer;

        private ConsoleColor color = ConsoleColor.White;

        public Queue<Tuple<ConsoleColor, string>> Buffer => buffer;

        public void SetColor(ConsoleColor color)
        {
            this.color = color;
        }

        public BufferedLogWriter(Queue<Tuple<ConsoleColor, string>> buffer)
        {
            this.buffer = buffer;
        }

        public void Flush() {}

        public void Close() {}

        public void Write(Log_Severity severity, string value)
        {
            ConsoleColor msgColor = color;

            switch (severity)
            {
                case Log_Severity.Fatal:
                    msgColor = ConsoleColor.Magenta;
                    break;
                case Log_Severity.Error:
                    msgColor = ConsoleColor.Red;
                    break;
                case Log_Severity.Warning:
                    msgColor = ConsoleColor.Yellow;
                    break;
                default: //Info and None
                    msgColor = color;
                    break;
            }

            buffer.Enqueue(new Tuple<ConsoleColor, string>(msgColor, value));
        }
    }
}

using System;
using System.Collections.Generic;
using AngryWasp.Logger;

namespace EMS
{
    public class BufferedLogWriter : ILogWriter
    {
        List<Tuple<ConsoleColor, string>> buffer;

        private ConsoleColor color = ConsoleColor.White;

        public List<Tuple<ConsoleColor, string>> Buffer => buffer;

        public void SetColor(ConsoleColor color)
        {
            this.color = color;
        }

        public BufferedLogWriter(List<Tuple<ConsoleColor, string>> buffer)
        {
            this.buffer = buffer;
        }

        public void Flush() {}

        public void Close() {}

        public void Write(Log_Severity severity, string value)
        {
            //do not clutter console with info messages
            if (severity == Log_Severity.Info)
                return;

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
                default:
                    msgColor = color;
                    break;
            }

            buffer.Add(new Tuple<ConsoleColor, string>(msgColor, value));
        }
    }
}

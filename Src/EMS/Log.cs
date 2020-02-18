using System;
using System.Collections.Generic;
using L = AngryWasp.Logger.Log;
using LS = AngryWasp.Logger.Log_Severity;

namespace EMS
{
    public static class Log
    {
        private static FileLogWriter fileWriter;
        private static BufferedLogWriter bufferWriter;

        public static List<Tuple<ConsoleColor, string>> Buffer => bufferWriter.Buffer;

        public static void Initialize(string logFile)
        {
            L.CreateInstance(false, logFile);

            if (!string.IsNullOrEmpty(logFile))
            {
                fileWriter = new FileLogWriter(logFile);
                L.Instance.AddWriter("file", fileWriter, false);
            }
            
            bufferWriter = new BufferedLogWriter(new List<Tuple<ConsoleColor, string>>());
            bufferWriter.SetColor(ConsoleColor.Cyan);
            L.Instance.AddWriter("buffer", bufferWriter, false);
        }

        public static void WriteConsole(string message) => L.Instance.Write(LS.None, message);
        public static void WriteInfo(string message) => L.Instance.Write(LS.Info, message);
        public static void WriteWarning(string message) => L.Instance.Write(LS.Warning, message);
        public static void WriteError(string message) => L.Instance.Write(LS.Error, message);
        public static void WriteFatal(string message) => L.Instance.Write(LS.Fatal, message);
    }
}
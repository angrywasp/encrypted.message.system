using System;
using System.IO;
using AngryWasp.Logger;

namespace EMS
{
    public class FileLogWriter : ILogWriter
    {
        StreamWriter output;

        public FileLogWriter(string logFilePath)
        {
            output = new StreamWriter(new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
        }

		public void Write(Log_Severity severity, string value)
		{
            //severity.none is reserved for the console writer
            if (severity == Log_Severity.None)
                return;

			output.WriteLine(value);
			output.Flush();
		}

        public void Flush()
        {
            output.Flush();
        }

        public void Close()
        {
            output.Close();
        }

        public void SetColor(ConsoleColor color)
        {
            //do nothing
        }
    }
}

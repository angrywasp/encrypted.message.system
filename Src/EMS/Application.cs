using System;
using System.Collections.Generic;
using System.Threading;
using AngryWasp.Helpers;
using AngryWasp.Logger;

namespace EMS
{
    public static class Application
    {
        public delegate bool CliFunc<T>(T arg);

        private static Dictionary<string, Tuple<string, CliFunc<string[]>>> commands = new Dictionary<string, Tuple<string, CliFunc<string[]>>>();

        public static Dictionary<string, Tuple<string, CliFunc<string[]>>> Commands => commands;

        public static void RegisterCommand(string key, string helpText, CliFunc<string[]> handler)
        {
            if (!commands.ContainsKey(key))
                commands.Add(key, new Tuple<string, CliFunc<string[]>>(helpText, handler));
        }

        public static void Start()
        {
            Log.Instance.RemoveWriter("console");
            bool noPrompt = false;
            List<char> enteredText = new List<char>();
            Queue<Tuple<ConsoleColor, string>> buffer = new Queue<Tuple<ConsoleColor, string>>();

            Thread t0 = new Thread(new ThreadStart( () =>
            {
                BufferedLogWriter blr = new BufferedLogWriter(buffer);
                Log.Instance.AddWriter("buffered", blr, true);

                while(true)
                {
                    Tuple<ConsoleColor, string> i = null;
                    if (!blr.Buffer.TryDequeue(out i))
                    {
                        Thread.Sleep(50);
                        continue;
                    }
                    Console.CursorLeft = 0;
                    Console.ForegroundColor = i.Item1;
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.CursorLeft = 0;
                    Console.WriteLine(i.Item2);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("> ");
                    Console.Write(new string(enteredText.ToArray()));
                }
            }));

            Thread t1 = new Thread(new ThreadStart( () =>
            {
                List<string> lines = new List<string>();
                int lineIndex = 0, lastLineIndex = 0;
                
                while (true)
                {
                    if (!noPrompt)
                        Console.Write("> ");

                    if (!Console.KeyAvailable)
                    {
                        noPrompt = true;
                        continue;
                    }

                    var key = Console.ReadKey();
                    noPrompt = false;
                    if (key.Key == ConsoleKey.UpArrow)
                    {
                        --lineIndex;
                        MathHelper.Clamp(ref lineIndex, 0, lines.Count - 1);

                        if (lineIndex == lastLineIndex)
                        {
                            noPrompt = true;
                            continue;
                        }

                        lastLineIndex = lineIndex;

                        Console.CursorLeft = 0;
                        Console.Write(new string(' ', Console.WindowWidth));

                        if (lineIndex < lines.Count)
                        {
                            Console.CursorLeft = 0;
                            Console.Write("> " + lines[lineIndex]);
                            noPrompt = true;
                        }

                        enteredText.Clear();
                        enteredText.AddRange(lines[lineIndex]);
                        
                        continue;
                    }
                    else if (key.Key == ConsoleKey.DownArrow)
                    {
                        ++lineIndex;
                        MathHelper.Clamp(ref lineIndex, 0, lines.Count - 1);

                        if (lineIndex == lastLineIndex)
                        {
                            noPrompt = true;
                            continue;
                        }

                        lastLineIndex = lineIndex;

                        Console.CursorLeft = 0;
                        Console.Write(new string(' ', Console.WindowWidth));

                        if (lineIndex < lines.Count)
                        {
                            Console.CursorLeft = 0;
                            Console.Write("> " + lines[lineIndex]);
                            noPrompt = true;
                        }

                        enteredText.Clear();
                        enteredText.AddRange(lines[lineIndex]);
                        
                        continue;
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (enteredText.Count > 0)
                        {
                            enteredText.RemoveAt(enteredText.Count - 1);
                            Console.Write("\b \b");
                        }

                        noPrompt = true;
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        string s = new string(enteredText.ToArray());
                        enteredText.Clear();
                        Console.WriteLine();
                        string[] args = Helpers.SplitArguments(s);
                        if (args.Length > 0)
                        {
                            if (commands.ContainsKey(args[0]))
                            {
                                if (!commands[args[0]].Item2.Invoke(args))
                                    Console.WriteLine("Command failed");
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(s))
                            lines.Add(s);

                        lineIndex = lines.Count;
                        lastLineIndex = lineIndex;
                    }
                    else if (key.KeyChar != '\u0000')
                    {
                        enteredText.Add(key.KeyChar);
                        noPrompt = true;
                    }
                }
            }));

            t0.Start();
            t1.Start();
            t0.Join();
            t1.Join();
        }
    }
}
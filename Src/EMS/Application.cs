using System;
using System.Collections.Generic;
using System.Threading;
using AngryWasp.Helpers;

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
            
            bool noPrompt = false;
            List<char> enteredText = new List<char>();

            Thread t0 = new Thread(new ThreadStart( () =>
            {
                while(true)
                {
                    if (Log.Buffer.Count == 0)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    //convert to array to make a copy before clearing the original list
                    var logMessages = Log.Buffer.ToArray();
                    Log.Buffer.Clear();

                    if (Console.CursorLeft != 0)
                    {
                        Console.Write("\r");
                        Console.Write(new string(' ', Console.BufferWidth));
                        Console.Write("\r");
                    }
                    
                    foreach (var m in logMessages)
                    {
                        Console.ForegroundColor = m.Item1;
                        Console.WriteLine(m.Item2);
                    }

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
                        int top = Console.CursorTop;

                        Console.CursorLeft = 0;
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.CursorLeft = 0;
                        Console.CursorTop = top;
                        Console.Write("> ");

                        if (lineIndex < lines.Count)
                        {
                            Console.Write(lines[lineIndex]);
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
                        int top = Console.CursorTop;

                        Console.CursorLeft = 0;
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.CursorLeft = 0;
                        Console.CursorTop = top;
                        Console.Write("> ");

                        if (lineIndex < lines.Count)
                        {
                            Console.Write(lines[lineIndex]);
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
                                    Log.WriteError("Command failed");
                            }
                            else
                                Log.WriteError("Unknown command");
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
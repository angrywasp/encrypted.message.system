using AngryWasp.Helpers;
using System;
using AngryWasp.Net;
using EMS.Commands.P2P;
using EMS.Commands.RPC;
using EMS.Commands.CLI;
using DnsClient;
using System.Linq;
using System.Collections.Generic;
using AngryWasp.Serializer;
using System.IO;

namespace EMS
{
    public static class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            CommandLineParser cmd = CommandLineParser.Parse(args);

            Console.Title = $"EMS {Version.VERSION}: {Version.CODE_NAME}";
            Serializer.Initialize();
            Config.Initialize(cmd["--config-file"] != null ? cmd["--config-file"].Value : Config.DEFAULT_CONFIG_FILE);

            if (cmd["--log-file"] != null)
                Config.User.LogFile = cmd["--log-file"].Value;

            if (cmd["--key-file"] != null)
                Config.User.KeyFile = cmd["--key-file"].Value;

            if (cmd["--p2p-port"] != null)
                Config.User.P2pPort = ushort.Parse(cmd["--p2p-port"].Value);

            if (cmd["--rpc-port"] != null)
                Config.User.RpcPort = ushort.Parse(cmd["--rpc-port"].Value);

            if (cmd["--rpc-ssl-port"] != null)
                Config.User.RpcSslPort = ushort.Parse(cmd["--rpc-ssl-port"].Value);

            Config.Save();

            Log.Initialize();
            Log.WriteConsole($"EMS {Version.VERSION}: {Version.CODE_NAME}");
            KeyRing.ReadKey();
            Console.Clear();
            if (Environment.GetEnvironmentVariable("TERM").StartsWith("xterm")) 
                Console.WriteLine("\x1b[3J");
            Console.CursorTop = 0;
            
            CommandCode.AddExternalHandler((b) =>
            {
                switch (b)
                {
                    case ShareMessage.CODE: return "ShareMessage";
                    case RequestMessagePool.CODE: return "RequestMessagePool";
                    case ShareMessageRead.CODE: return "ShareMessageRead";
                    default: return "Unknown";
                }
            });
            
            CommandProcessor.RegisterCommand(ShareMessage.CODE, ShareMessage.GenerateResponse);
            CommandProcessor.RegisterCommand(ShareMessageRead.CODE, ShareMessageRead.GenerateResponse);
            CommandProcessor.RegisterCommand(RequestMessagePool.CODE, RequestMessagePool.GenerateResponse);
            CommandProcessor.RegisterDefaultCommands();

            RpcServer.RegisterCommand<Commands.RPC.GetAddress.JsonRequest>("get_address", Commands.RPC.GetAddress.Handle);
            RpcServer.RegisterCommand<object>("get_message_count", GetMessageCount.Handle);
            RpcServer.RegisterCommand<object>("get_message_details", GetMessageDetails.Handle);
            RpcServer.RegisterCommand<Commands.RPC.GetMessage.JsonRequest>("get_message", GetMessage.Handle);
            RpcServer.RegisterCommand<Commands.RPC.SendMessage.JsonRequest>("send_message", Commands.RPC.SendMessage.Handle);

            if (cmd["--no-reconnect"] != null)
                Config.User.NoReconnect = true;
            else if (cmd["--seed-nodes"] != null)
            {
                //todo: check for formatting errors
                string[] nodes = cmd["--seed-nodes"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var n in nodes)
                {
                    string[] node = n.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    string host = node[0];
                    ushort port = AngryWasp.Net.Config.DEFAULT_PORT;
                    if (node.Length > 1)
                        ushort.TryParse(node[1], out port);

                    AngryWasp.Net.Config.AddSeedNode(host, port);

                    Log.WriteConsole($"Added seed node {host}:{port}");
                }
            }

            Helpers.AddSeedFromDns();

            new Server().Start(Config.User.P2pPort);
            new RpcServer().Start(Config.User.RpcPort, Config.User.RpcSslPort);

            if (!Config.User.NoReconnect)
                Client.ConnectToSeedNodes();

            TimedEvents.Initialize();
            Application.RegisterCommands();
            Application.Start();
        }
    }
}

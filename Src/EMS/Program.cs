using AngryWasp.Helpers;
using System;
using AngryWasp.Net;
using EMS.Commands.P2P;
using EMS.Commands.RPC;
using AngryWasp.Serializer;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

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
            Config.Initialize(cmd["config-file"] != null ? cmd["config-file"].Value : Config.DEFAULT_CONFIG_FILE);
            if (!ConfigMapper.Process(cmd))
                return;

            Config.Save();

            Log.Initialize();
            Log.WriteConsole($"EMS {Version.VERSION}: {Version.CODE_NAME}");
            if (!Config.User.RelayOnly)
                KeyRing.ReadKey(cmd["password"] != null ? cmd["password"].Value : null);
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

            foreach (var seedNode in Config.User.SeedNodes)
            {
                string[] node = seedNode.Split(':', StringSplitOptions.RemoveEmptyEntries);
                string host = node[0];
                ushort port = AngryWasp.Net.Config.DEFAULT_PORT;
                if (node.Length > 1)
                    ushort.TryParse(node[1], out port);

                AngryWasp.Net.Config.AddSeedNode(host, port);
                Log.WriteConsole($"Added seed node {host}:{port}");
            }

            if (!Config.User.NoDnsSeeds)
                Helpers.AddSeedFromDns();

            new Server().Start(Config.User.P2pPort);
            new RpcServer().Start(Config.User.RpcPort, Config.User.RpcSslPort);

            Client.ConnectToSeedNodes();

            TimedEventManager.RegisterEvents(Assembly.GetExecutingAssembly());
            Application.RegisterCommands();
            Application.Start();
        }
    }
}

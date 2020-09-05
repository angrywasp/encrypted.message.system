using AngryWasp.Helpers;
using System;
using AngryWasp.Net;
using EMS.Commands.P2P;
using EMS.Commands.RPC;
using AngryWasp.Serializer;
using System.Reflection;
using System.IO;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using AngryWasp.Cli.DefaultCommands;
using AngryWasp.Json.Rpc;

namespace EMS
{
    public static class MainClass
    {
        [STAThread]
        public static void Main(string[] rawArgs)
        {
            Arguments args = Arguments.Parse(rawArgs);

            Console.Title = $"EMS {Version.VERSION}: {Version.CODE_NAME}";
            Serializer.Initialize();
            Config.Initialize(args["config-file"] != null ? args["config-file"].Value : Config.DEFAULT_CONFIG_FILE);
            if (!ConfigMapper.Process(args))
                return;

            Config.Save();

            Log.Initialize();
            Log.WriteConsole($"EMS {Version.VERSION}: {Version.CODE_NAME}");
            if (!Config.User.RelayOnly)
                KeyRing.ReadKey(args["password"] != null ? args["password"].Value : null);

            new Clear().Handle(null);
            
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

            if (Config.User.CacheIncoming)
                File.Create(Config.User.CacheFile);

            new Server().Start(Config.User.P2pPort);

            JsonRpcServer server = new JsonRpcServer(Config.User.RpcPort);
            server.RegisterCommands();
            server.Start();

            Client.ConnectToSeedNodes();

            TimedEventManager.RegisterEvents(Assembly.GetExecutingAssembly());
            Application.RegisterCommands();
            Application.Start();
        }
    }
}

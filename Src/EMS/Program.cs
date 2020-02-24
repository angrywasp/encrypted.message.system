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
            bool noReconnect = false;

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
                noReconnect = true;
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

            AddSeedFromDns();

            new Server().Start(Config.User.P2pPort);
            new RpcServer().Start(Config.User.RpcPort, Config.User.RpcSslPort);

            if (!noReconnect)
                Client.ConnectToSeedNodes();

#region Timed events

            //Delete expirated messages, every 5 minutes
            TimedEventManager.Add("deleteexpired", () =>
            {
                HashSet<HashKey16> delete = new HashSet<HashKey16>();

                foreach (var m in MessagePool.Messages)
                {
                    if (m.Value.IsExpired())
                        delete.Add(m.Key);
                }

                foreach (var k in delete)
                {
                    Message m;
                    MessagePool.Messages.TryRemove(k, out m);
                    Log.WriteConsole($"Message {m.Key} expired");
                    if (MessagePool.OutgoingMessages.Contains(k))
                        MessagePool.OutgoingMessages.Remove(k);
                }
            }, 300 * 1000);

            //Check for new seeds via DNS every hour
            TimedEventManager.Add("seeds", () =>
            {
                AddSeedFromDns();
            }, 3600 * 1000);

            // Poll the seed nodes for more peers if disconnected
            TimedEventManager.Add("reconnect", () =>
            {
                if (ConnectionManager.Count == 0 && !noReconnect)
                    Client.ConnectToSeedNodes();
            }, 30 * 1000);

            // Exchange peer lists with connected peers. Every 10 minutes.
            // We can have a long period here because we have the reconnect event 
            // running every 30 seconds in case we lose all our peer list connections
            // TODO: we should request sequentially to prevent duplicate data being unnecessarily
            // sent around the network
            TimedEventManager.Add("peers", () =>
            {
                ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
                {
                    c.Write(ExchangePeerList.GenerateRequest(true).ToArray());
                });
            }, 600 * 1000);

            // Request messages from other nodes. Every 5 minutes
            // TODO: we should request sequentially to prevent duplicate data being unnecessarily
            // sent around the network
            TimedEventManager.Add("messages", () =>
            {
                ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
                {
                    c.Write(RequestMessagePool.GenerateRequest(true).ToArray());
                });
            }, 60 * 1000);

#endregion

            Application.RegisterCommand("exit", "Exit the program", Exit.Handle);
            Application.RegisterCommand("send", "Send a message. Usage: send <address> <message>", Commands.CLI.SendMessage.Handle);
            Application.RegisterCommand("address", "Prints your messaging address", Commands.CLI.GetAddress.Handle);
            Application.RegisterCommand("new_address", "Discards your current address and creates a new one", Commands.CLI.NewAddress.Handle);
            Application.RegisterCommand("peers", "Print a list of connected peers", Commands.CLI.GetPeers.Handle);
            Application.RegisterCommand("messages", "Print the message pool", Commands.CLI.GetMessages.Handle);
            Application.RegisterCommand("read", "Read a message. Usage: read <message_hash>", Commands.CLI.ReadMessage.Handle);
            Application.RegisterCommand("flag", "Mark a message as read. Usage: flag <message_hash>", Commands.CLI.MarkMessageRead.Handle);
            Application.RegisterCommand("get", "Get a config option value. Usage: get <param>", Commands.CLI.GetConfig.Handle);
            Application.RegisterCommand("set", "Set a config option value. Usage: set <param> <value>", Commands.CLI.SetConfig.Handle);
            Application.RegisterCommand("clear", "Clear the console", Commands.CLI.Clear.Handle);
            Application.RegisterCommand("time", "Display the current UTC time", Commands.CLI.Time.Handle);
            Application.RegisterCommand("last", "Display the last received message", Commands.CLI.LastMessage.Handle);

            Application.RegisterCommand("sync", "Manually sync new messages from your connected peers", (c) =>
            {
                // TODO: we should request sequentially to prevent duplicate data being unnecessarily
                // sent around the network
                ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
                {
                    c.Write(RequestMessagePool.GenerateRequest(true).ToArray());
                });

                return true;
            });

            Application.RegisterCommand("help", "Print the help text", (c) =>
            {
                Console.WriteLine();
                foreach (var cmd in Application.Commands)
                    Console.WriteLine($"{cmd.Key}: {cmd.Value.Item1}");
                return true;
            });

            Application.Start();
        }

        private static void AddSeedFromDns()
        {
            var client = new LookupClient();
            var records = client.Query("seed.angrywasp.net.au", QueryType.TXT).Answers;
                
            foreach (var r in records)
            {
                string txt = ((DnsClient.Protocol.TxtRecord)r).Text.ToArray()[0];

                string[] node = txt.Split(':', StringSplitOptions.RemoveEmptyEntries);
                string host = node[0];
                ushort port = AngryWasp.Net.Config.DEFAULT_PORT;
                if (node.Length > 1)
                    ushort.TryParse(node[1], out port);

                if (AngryWasp.Net.Config.HasSeedNode(host, port))
                    continue;

                AngryWasp.Net.Config.AddSeedNode(host, port);
                Log.WriteConsole($"Added seed node {host}:{port}");
            }
        }
    }
}

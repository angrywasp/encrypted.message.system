﻿using AngryWasp.Helpers;
using System;
using AngryWasp.Net;
using EMS.Commands.P2P;
using EMS.Commands.RPC;
using EMS.Commands.CLI;
using DnsClient;
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

            Log.Create(cmd["--log-file"] != null ? cmd["--log-file"].Value : null);

            bool noReconnect = false;

            KeyRing.LoadFromFile(cmd["--key-file"] != null ? cmd["--key-file"].Value : null);

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

            ushort portStart = (ushort)new MersenneTwister(MathHelper.Random.GenerateRandomSeed()).NextUInt(10000, 20000);

            ushort p2pPort = (cmd["--p2p-port"] != null) ? ushort.Parse(cmd["--p2p-port"].Value) : ++portStart;
            ushort rpcPort = (cmd["--rpc-port"] != null) ? ushort.Parse(cmd["--rpc-port"].Value) : ++portStart;
            ushort rpcSslPort = (cmd["--rpc-ssl-port"] != null) ? ushort.Parse(cmd["--rpc-ssl-port"].Value) : ++portStart;

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

            new Server().Start(p2pPort);
            new RpcServer().Start(rpcPort, rpcSslPort);

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
            Application.RegisterCommand("send", "Send a message. Usage: Send <address> \"<message>\"", Commands.CLI.SendMessage.Handle);
            Application.RegisterCommand("address", "Prints your messaging address", Commands.CLI.GetAddress.Handle);
            Application.RegisterCommand("peers", "Print a list of connected peers", Commands.CLI.GetPeers.Handle);
            Application.RegisterCommand("messages", "Print the message pool", Commands.CLI.GetMessages.Handle);
            Application.RegisterCommand("read", "Read a message. Usage: read <message_hash>", Commands.CLI.ReadMessage.Handle);
            Application.RegisterCommand("flag", "Mark a message as read. Usage: flag <message_hash>", Commands.CLI.MarkMessageRead.Handle);

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

                foreach (var s in AngryWasp.Net.Config.SeedNodes)
                    if (s.Host == host && s.Port == port)
                        continue;

                AngryWasp.Net.Config.AddSeedNode(host, port);
                Log.WriteConsole($"Added seed node {host}:{port}");
            }
        }
    }
}

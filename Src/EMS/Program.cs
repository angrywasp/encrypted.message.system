using AngryWasp.Logger;
using AngryWasp.Helpers;
using System;
using AngryWasp.Net;
using EMS.Commands.P2P;
using EMS.Commands.RPC;
using EMS.Commands.CLI;

namespace EMS
{
    public static class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            CommandLineParser cmd = CommandLineParser.Parse(args);

            if (cmd["--log-file"] != null)
                Log.CreateInstance(false, cmd["--log-file"].Value);
            else
                Log.CreateInstance(true);

            bool isSeedNode = false;

            KeyRing.Initialize();
            
            CommandProcessor.RegisterCommand(ShareMessage.CODE, ShareMessage.GenerateResponse);
            CommandProcessor.RegisterCommand(RequestMessagePool.CODE, RequestMessagePool.GenerateResponse);
            CommandProcessor.RegisterDefaultCommands();

            RpcServer.RegisterCommand<Commands.RPC.GetAddress.JsonRequest>("get_address", Commands.RPC.GetAddress.Handle);
            RpcServer.RegisterCommand<object>("get_message_count", GetMessageCount.Handle);
            RpcServer.RegisterCommand<object>("get_message_details", GetMessageDetails.Handle);
            RpcServer.RegisterCommand<Commands.RPC.SendMessage.JsonRequest>("send_message", Commands.RPC.SendMessage.Handle);

            int portStart = (int)new MersenneTwister(MathHelper.Random.GenerateRandomSeed()).NextUInt(10000, 20000);

            int p2pPort = (cmd["--p2p-port"] != null) ? int.Parse(cmd["--p2p-port"].Value) : ++portStart;
            int rpcPort = (cmd["--rpc-port"] != null) ? int.Parse(cmd["--rpc-port"].Value) : ++portStart;
            int rpcSslPort = (cmd["--rpc-ssl-port"] != null) ? int.Parse(cmd["--rpc-ssl-port"].Value) : ++portStart;

            if (cmd["--seed-node"] != null)
                isSeedNode = true;

            new Server().Start(p2pPort);
            new RpcServer().Start(rpcPort, rpcSslPort);

            if (!isSeedNode)
                Client.ConnectToSeedNodes();

            #region Timed events

            // Poll the seed nodes for more peers if disconnected
            TimedEventManager.Add("reconnect", () =>
            {
                if (ConnectionManager.Count == 0 && !isSeedNode)
                    Client.ConnectToSeedNodes();
            }, 30 * 1000);

            // Exchange peer lists with connected peers
            TimedEventManager.Add("peers", () =>
            {
                ConnectionManager.ForEach(Direction.Outgoing, (c) =>
                {
                    c.Write(ExchangePeerList.GenerateRequest(true));
                });
            }, 15 * 1000);

            // Request messages from other nodes
            TimedEventManager.Add("messages", () =>
            {
                ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
                {
                    c.Write(RequestMessagePool.GenerateRequest(true));
                });
            }, 15 * 1000);

            #endregion

            Application.RegisterCommand("exit", Exit.Handle);
            Application.RegisterCommand("send", Commands.CLI.SendMessage.Handle);
            Application.RegisterCommand("address", Commands.CLI.GetAddress.Handle);
            Application.RegisterCommand("peers", Commands.CLI.GetPeers.Handle);
            Application.RegisterCommand("messages", Commands.CLI.GetMessages.Handle);
            Application.RegisterCommand("read", Commands.CLI.ReadMessage.Handle);
            Application.Start();
        }
    }
}

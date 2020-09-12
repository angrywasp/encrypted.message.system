using System;
using System.Collections.Generic;
using System.Linq;
using AngryWasp.Cli;
using AngryWasp.Net;
using DnsClient;

namespace EMS
{
    public static class Helpers
    {
        public static string PopWord(ref string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
                
            int index = input.IndexOf(' ');
            if (index == -1)
            {
                string ret = input;
                input = string.Empty;
                return ret;
            }
            else
            {
                string ret = input.Substring(0, index);
                input = input.Remove(0, ret.Length).TrimStart();
                return ret;
            }
        }

        public static void AddSeedFromDns()
        {
            var client = new LookupClient();
            var records = client.Query("seed.angrywasp.net.au", QueryType.TXT).Answers;
                
            foreach (var r in records)
            {
                string txt = ((DnsClient.Protocol.TxtRecord)r).Text.ToArray()[0];

                string[] node = txt.Split(new char[] { ':'}, StringSplitOptions.RemoveEmptyEntries);
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

        public static void MessageAll(byte[] request)
        {
            List<Connection> disconnected = new List<Connection>();

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
            {
                if (!c.Write(request))
                    disconnected.Add(c);
            });

            foreach (var c in disconnected)
                ConnectionManager.Remove(c);
        }
    }
}
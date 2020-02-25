using System.Collections.Generic;
using AngryWasp.Net;
using EMS.Commands.P2P;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("sync", "Manually sync new messages from your connected peers")]
    public class Sync : IApplicationCommand
    {
        public bool Handle(string command)
        {
            List<Connection> disconnected = new List<Connection>();
            
            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
            {
                if (!c.Write(RequestMessagePool.GenerateRequest(true).ToArray()))
                    disconnected.Add(c);
            });

            foreach (Connection c in disconnected)
                ConnectionManager.Remove(c);
                
            return true;
        }
    }
}
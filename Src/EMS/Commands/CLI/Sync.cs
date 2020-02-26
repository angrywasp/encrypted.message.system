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
            Helpers.MessageAll(RequestMessagePool.GenerateRequest(true).ToArray());
            return true;
        }
    }
}
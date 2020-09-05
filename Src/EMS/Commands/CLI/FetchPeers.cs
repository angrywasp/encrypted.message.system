using AngryWasp.Cli;
using AngryWasp.Net;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("fetch_peers", "Ask your peers for more connections")]
    public class FetchPeers : IApplicationCommand
    {
        public bool Handle(string command)
        {
            Helpers.MessageAll(ExchangePeerList.GenerateRequest(true).ToArray());
            return true;
        }
    }
}
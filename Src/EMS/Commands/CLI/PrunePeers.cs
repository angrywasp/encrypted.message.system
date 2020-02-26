namespace EMS.Commands.CLI
{
    [ApplicationCommand("prune_peers", "Ping all nodes and remove dead connections")]
    public class PrunePeerList : IApplicationCommand
    {
        public bool Handle(string command)
        {
            Helpers.MessageAll(AngryWasp.Net.Ping.GenerateRequest().ToArray());
            return true;
        }
            
    }
}
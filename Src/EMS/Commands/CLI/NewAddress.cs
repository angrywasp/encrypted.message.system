using AngryWasp.Cli;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("new_address", "Discards your current address and creates a new one")]
    public class NewAddress : IApplicationCommand
    {
        public bool Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }
            
            string file = Helpers.PopWord(ref command);
            KeyRing.NewKey();
            return true;
        }
    }
}
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("new_address", "Discards your current address and creates a new one")]
    public class NewAddress : IApplicationCommand
    {
        public bool Handle(string command)
        {
            string file = Helpers.PopWord(ref command);
            KeyRing.NewKey();
            return true;
        }
    }
}
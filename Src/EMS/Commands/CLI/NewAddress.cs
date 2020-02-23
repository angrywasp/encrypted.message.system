using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    public class NewAddress
    {
        public static bool Handle(string command)
        {
            string file = Helpers.PopWord(ref command);
            KeyRing.NewKey();
            return true;
        }
    }
}
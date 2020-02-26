using System;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("address", "Prints your messaging address")]
    public class GetAddress : IApplicationCommand
    {
        public bool Handle(string command)
        {
            if (Config.User.RelayOnly)
            {
                Log.WriteError($"Command not allowed with the --relay-only flag");
                return false;
            }
            
            Console.WriteLine(Base58.Encode(KeyRing.PublicKey));
            return true;
        }
    }
}
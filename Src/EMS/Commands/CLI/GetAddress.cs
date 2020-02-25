using System;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("address", "Prints your messaging address")]
    public class GetAddress : IApplicationCommand
    {
        public bool Handle(string command)
        {
            Console.WriteLine(Base58.Encode(KeyRing.PublicKey));
            return true;
        }
    }
}
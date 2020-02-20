using System;

namespace EMS.Commands.CLI
{
    public class GetAddress
    {
        public static bool Handle(string command)
        {
            Console.WriteLine(Base58.Encode(KeyRing.PublicKey));
            return true;
        }
    }
}
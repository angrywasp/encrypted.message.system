using AngryWasp.Cryptography;

namespace EMS
{
    public class SlowHash
    {
        public static byte[] Hash(byte[] input) => Keccak.Hash256(input);
    }
}
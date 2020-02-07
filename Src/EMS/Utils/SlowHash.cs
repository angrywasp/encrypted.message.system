using AngryWasp.Cryptography;

namespace EMS
{
    public class SlowHash
    {
        public static HashKey32 Hash(HashKey16 input) => Keccak.Hash256(input.ToByte());

        public static HashKey32 Hash(byte[] input) => Keccak.Hash256(input);
    }
}
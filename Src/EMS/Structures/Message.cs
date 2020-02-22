using System.Linq;
using AngryWasp.Helpers;

namespace EMS
{
    public class Message
    {
        public HashKey16 Key { get; set; } = HashKey16.Empty;
        public HashKey32 Hash { get; set; } = HashKey32.Empty;
        public byte[] Data { get; set; } = null;
        public uint Timestamp { get; set; } = 0;
        public uint Expiration { get; set; } = 0;
        public string Address { get; set; } =  string.Empty;
        public string DecryptedMessage { get; set; } =  string.Empty;

        public ReadProof ReadProof { get; set; } = null;
        
        public bool IsDecrypted
        {
            get { return !string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(DecryptedMessage); }
        }

        public HashKey32 ExtractReadProofHash() => Data.Skip(113).Take(32).ToArray();

        public bool IsExpired()
        {
            ulong now = DateTimeHelper.TimestampNow();
            ulong expireTime = Timestamp + Expiration;

            if (now > expireTime)
                return true;
            
            return false;
        }
    }
}
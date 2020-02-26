using System.Linq;
using System.Text;
using AngryWasp.Helpers;

namespace EMS
{
    public enum Message_Type : byte
    {
        Text = 0,
        Invalid = 1
    }

    public class Message
    {
        public HashKey16 Key { get; set; } = HashKey16.Empty;
        public HashKey32 Hash { get; set; } = HashKey32.Empty;
        public byte[] Data { get; set; } = null;
        public byte MessageVersion { get; set; } = 0;
        public Message_Type MessageType { get; set; } = Message_Type.Invalid;
        public uint Timestamp { get; set; } = 0;
        public uint Expiration { get; set; } = 0;
        public string Address { get; set; } =  string.Empty;
        public byte[] DecryptedData { get; set; } = null;

        public ReadProof ReadProof { get; set; } = new ReadProof();
        
        public bool IsDecrypted
        {
            get { return !string.IsNullOrEmpty(Address) && DecryptedData != null; }
        }

        public HashKey32 ExtractReadProofHash() => Data.Skip(114).Take(32).ToArray();

        public bool IsExpired()
        {
            ulong expireTime = Timestamp + Expiration;

            if (DateTimeHelper.TimestampNow > expireTime)
                return true;
            
            return false;
        }

        public string ParseDecryptedData()
        {
            if (MessageType >= Message_Type.Invalid)
                return $"MessageType {((byte)MessageType).ToString()} is invalid";

            switch (MessageType)
            {
                case Message_Type.Text:
                    return Encoding.ASCII.GetString(DecryptedData);
                default:
                    return $"MessageType {((byte)MessageType).ToString()} is not supported";
            }
        }
    }
}
using System.Collections.Generic;
using System.Text;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using AngryWasp.Net;
using EMS.Commands.P2P;

namespace EMS
{
    public class MessagePoolItem
    {
        private ulong timestamp;
        private byte[] message;
        private bool markedRead;

        public ulong TimeStamp => timestamp;

        public byte[] Message => message;

        public bool MarkedRead => markedRead;

        public MessagePoolItem(ulong ts, byte[] m)
        {
            timestamp = ts;
            message = m;
        }
    }

    public class DecryptedMessagePoolItem
    {
        private ulong timestamp;
        private string sender;
        private string message;

        public ulong TimeStamp => timestamp;

        public string Sender => sender;

        public string Message => message;

        public DecryptedMessagePoolItem(ulong ts, string s, string m)
        {
            timestamp = ts;
            sender = s;
            message = m;
        }
    }

    public static class MessagePool
    {
        private static Dictionary<HashKey, MessagePoolItem> messages = new Dictionary<HashKey, MessagePoolItem>();
        private static Dictionary<HashKey, DecryptedMessagePoolItem> decryptedMessages = new Dictionary<HashKey, DecryptedMessagePoolItem>();

        public static Dictionary<HashKey, MessagePoolItem> Messages => messages;

        public static Dictionary<HashKey, DecryptedMessagePoolItem> DecryptedMessages => decryptedMessages;

        public static int Count => messages.Count;

        public static int DecryptedCount => decryptedMessages.Count;

        public static HashKey Send(string address, string message)
        {
            byte[] encrypted = KeyRing.EncryptMessage(Encoding.ASCII.GetBytes(message), address);
            ulong ts = DateTimeHelper.TimestampNow();

            //todo: we can optimize this a bit by re-implementing Add here as we don't really need to decrypt the message
            //to add it to the decrypted message pool.
            List<byte> data = new List<byte>();
            data.AddRange(BitShifter.ToByte(ts));
            data.AddRange(encrypted);
            HashKey hk = Add(data.ToArray());

            if (hk == HashKey.Empty) //was not added to the local pool. do not transmit
                return HashKey.Empty;

            byte[] req = ShareMessage.GenerateRequest(true, ts, encrypted);

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
            {
                c.Write(req);
            });

            return hk;
        }

        public static HashKey Add(byte[] message)
        {
            HashKey key = Keccak.Hash128(message);
            if (messages.ContainsKey(key))
                return HashKey.Empty; //already in pool

            ulong timestamp = BitShifter.ToULong(message);
            string sender = null;
            byte[] msg = KeyRing.DecryptMessage(message, 8, out sender);

            if (msg != null)
                decryptedMessages.Add(key, new DecryptedMessagePoolItem(timestamp, sender, Encoding.ASCII.GetString(msg)));

            messages.Add(key, new MessagePoolItem(timestamp, message));
            return key;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using AngryWasp.Net;
using EMS.Commands.P2P;

namespace EMS
{
    public static class MessagePool
    {
        private static Dictionary<HashKey, Tuple<ulong, byte[]>> messages = new Dictionary<HashKey, Tuple<ulong, byte[]>>();
        private static Dictionary<HashKey, Tuple<ulong, string, string>> decryptedMessages = new Dictionary<HashKey, Tuple<ulong, string, string>>();

        public static Dictionary<HashKey, Tuple<ulong, byte[]>> Messages => messages;

        public static Dictionary<HashKey, Tuple<ulong, string, string>> DecryptedMessages => decryptedMessages;

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
                decryptedMessages.Add(key, new Tuple<ulong, string, string>(timestamp, sender, Encoding.ASCII.GetString(msg)));

            messages.Add(key, new Tuple<ulong, byte[]>(timestamp, message));
            return key;
        }
    }
}
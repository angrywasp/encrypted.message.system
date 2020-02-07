using System.Collections.Generic;
using System.Text;
using AngryWasp.Helpers;
using AngryWasp.Net;
using EMS.Commands.P2P;
using System.Linq;
using System.Collections.Concurrent;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System.Diagnostics;
using System;

namespace EMS
{
    public class ProvableMessage
    {
        private HashKey16 readProofKey = HashKey16.Empty;
        private HashKey32 readProofHash = HashKey32.Empty;

        public bool HasReadProof()
        {
            if (readProofKey.IsNullOrEmpty() || readProofHash.IsNullOrEmpty())
                return false;

            return true;
        }

        public virtual void SetReadProof(HashKey16 key, HashKey32 hash)
        {
            readProofKey = key;

            if (!hash.IsNullOrEmpty())
                readProofHash = hash;
            else
                readProofHash = GenerateHash(key);
        }

        public bool GetReadProof(out HashKey16 key, out HashKey32 hash)
        {
            key = readProofKey;
            hash = readProofHash;

            return HasReadProof();
        }

        public static HashKey32 GenerateHash(HashKey16 key) => SlowHash.Hash(key);
    }

    public class EncryptedMessage : ProvableMessage
    {
        private byte[] message;
        
        public byte[] Message => message;

        public EncryptedMessage(byte[] m)
        {
            message = m;
        }

        public HashKey32 ExtractReadProofHash()
        {
            return message.Skip(69).Take(32).ToArray();
        }

        public override void SetReadProof(HashKey16 key, HashKey32 hash)
        {
            base.SetReadProof(key, hash);
            //prune the message pool by truncating the message
            message = null;
        }
    }

    public class IncomingMessage : ProvableMessage
    {
        private ulong timestamp;
        private string sender;
        private string message;
        private bool isRead;

        public ulong TimeStamp => timestamp;
        public string Sender => sender;
        public string Message => message;
        public bool IsRead => isRead;

        public IncomingMessage(ulong timestamp, string sender, string message)
        {
            this.timestamp = timestamp;
            this.sender = sender;
            this.message = message;
        }

        public void SetAsRead(bool r)
        {
            isRead = r;
        }
    }

    public class OutgoingMessage
    {
        private ulong timestamp;
        private string recipient;
        private string message;

        public ulong TimeStamp => timestamp;

        public string Recipient => recipient;

        public string Message => message;

        public OutgoingMessage(ulong ts, string r, string m)
        {
            timestamp = ts;
            recipient = r;
            message = m;
        }
    }

    public static class MessagePool
    {
        private static ConcurrentDictionary<HashKey16, EncryptedMessage> encryptedMessages = new ConcurrentDictionary<HashKey16, EncryptedMessage>();
        private static ConcurrentDictionary<HashKey16, IncomingMessage> incomingMessages = new ConcurrentDictionary<HashKey16, IncomingMessage>();
        private static ConcurrentDictionary<HashKey16, OutgoingMessage> outgoingMessages = new ConcurrentDictionary<HashKey16, OutgoingMessage>();

        public static ConcurrentDictionary<HashKey16, EncryptedMessage> EncryptedMessages => encryptedMessages;

        public static ConcurrentDictionary<HashKey16, IncomingMessage> IncomingMessages => incomingMessages;

        public static ConcurrentDictionary<HashKey16, OutgoingMessage> OutgoingMessages => outgoingMessages;

        public static int IncomingCount => incomingMessages.Count;

        public static int OutgoingCount => outgoingMessages.Count;

        public static bool Send(string address, string message, uint expiration, out HashKey16 key)
        {
            key = HashKey16.Empty;

            if (message.Length > (16 * 1024))
            {
                Log.WriteWarning("Message exceeds the 16kb limit");
                return false;
            }

            ulong timestamp = DateTimeHelper.TimestampNow();

            List<byte> msgBytes = BitShifter.ToByte(timestamp).ToList();
            msgBytes.AddRange(AngryWasp.Cryptography.Helper.GenerateSecureBytes(16));
            msgBytes.AddRange(Encoding.ASCII.GetBytes(message));

            byte[] encResult;
            byte[] addressXor;
            if (!KeyRing.EncryptMessage(msgBytes.ToArray(), address, out encResult, out addressXor))
                return false;

            //prepend a uint width of bytes to the array for a hashing nonce and the expiration time
            byte[] finalMessage = new byte[4] {0, 0, 0, 0};
            finalMessage = finalMessage.Concat(BitShifter.ToByte((uint)DateTimeHelper.TimestampNow() + expiration)).ToArray();
            finalMessage = finalMessage.Concat(encResult).ToArray();

            uint x = MathHelper.Random.GenerateRandomSeed();
            ulong difficulty = expiration * 1024;
            byte[] messageHash = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while(true)
            {
                Buffer.BlockCopy(BitShifter.ToByte(x), 0, finalMessage, 0, 4);
                messageHash = SlowHash.Hash(finalMessage).ToByte();
                if (Validator.CheckHash(messageHash, difficulty))
                    break;

                ++x;
            }

            sw.Stop();

            Log.WriteConsole($"Message took {sw.ElapsedMilliseconds / 1000.0} seconds to hash");

            finalMessage = messageHash.Concat(finalMessage).ToArray();

            key = HashKey16.Make(finalMessage);

            //add it to a special list of outgoing messages to make sorting messages for display easier
            outgoingMessages.TryAdd(key, new OutgoingMessage(timestamp, address, message));

            if (!encryptedMessages.TryAdd(key, new EncryptedMessage(finalMessage)))
                return false;

            byte[] req = ShareMessage.GenerateRequest(true, finalMessage);

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
            {
                c.Write(req);
            });

            return true;
        }

        public static bool MarkMessageRead(HashKey16 key)
        {
            IncomingMessage i = null;
            EncryptedMessage e = null;

            //we can't do this without the decrypted message
            //what we effectively do is copy the read proof key and hash from the
            //decrypted message to the encrypted message, which is then propagated through the network

            if (!incomingMessages.TryGetValue(key, out i))
            {
                Log.WriteWarning($"Could not mark decrypted message with key {key.ToString()} as read");
                return false;
            }

            if (!encryptedMessages.TryGetValue(key, out e))
            {
                Log.WriteWarning($"Could not mark encrypted message with key {key.ToString()} as read");
                return false;
            }

            //Get the read proof from the decrypted message
            HashKey16 readProofKey;
            HashKey32 readProofHash;
            i.GetReadProof(out readProofKey, out readProofHash);

            //redundant error check. should never be true, so kill the program if it is
            if (readProofKey.IsNullOrEmpty() || readProofHash.IsNullOrEmpty())
                Log.WriteFatal("Decrypted read proof is invalid. This should never happen");
            
            //copy the read proof to the encrypted message. This will be picked up by anyone
            //who doesn't get this message on the next message pool sync
            e.SetReadProof(readProofKey, readProofHash);
            i.SetAsRead(true);

            List<byte> message = new List<byte>();
            message.AddRange(key);
            message.AddRange(readProofKey);

            byte[] req = ShareMessageRead.GenerateRequest(true, message.ToArray());

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (c) =>
            {
                c.Write(req);
            });

            return true;
        }
    }
}
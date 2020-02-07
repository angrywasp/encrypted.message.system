using System.Collections.Generic;
using System.Text;
using AngryWasp.Helpers;
using AngryWasp.Net;
using EMS.Commands.P2P;
using System.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System;
using System.IO;

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
            return message.Skip(113).Take(32).ToArray();
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
        private uint timestamp;
        private uint expiration;
        private string sender;
        private string message;
        private bool isRead;

        public uint TimeStamp => timestamp;
        public uint Expiration => expiration;
        public string Sender => sender;
        public string Message => message;
        public bool IsRead => isRead;

        public IncomingMessage(uint timestamp, uint expiration, string sender, string message)
        {
            this.timestamp = timestamp;
            this.expiration = expiration;
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
        private uint timestamp;
        private string recipient;
        private string message;

        public uint TimeStamp => timestamp;

        public string Recipient => recipient;

        public string Message => message;

        public OutgoingMessage(uint ts, string r, string m)
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
                Log.WriteError("Message exceeds the 16kb limit");
                return false;
            }

            if (expiration < GlobalConfig.MIN_MESSAGE_EXPIRATION)
            {
                Log.WriteError($"Message expiration is less than the minimum {GlobalConfig.MIN_MESSAGE_EXPIRATION}");
                return false;
            }

            byte[] encryptionResult, signature, addressXor;

            byte[] readProofKey = AngryWasp.Cryptography.Helper.GenerateSecureBytes(16);
            byte[] readProofHash = ProvableMessage.GenerateHash(readProofKey);

            byte[] msg = readProofKey
                .Concat(Encoding.ASCII.GetBytes(message)).ToArray();

            if (!KeyRing.EncryptMessage(msg, address, out encryptionResult, out signature, out addressXor))
                return false;

            byte[] finalMessage = new byte[4] {0, 0, 0, 0}
                .Concat(BitShifter.ToByte(expiration))
                .Concat(BitShifter.ToByte((ushort)signature.Length))
                .Concat(BitShifter.ToByte((ushort)encryptionResult.Length))
                .Concat(addressXor)
                .Concat(readProofHash)
                .Concat(signature)
                .Concat(encryptionResult).ToArray();

            uint x = MathHelper.Random.GenerateRandomSeed();
            ulong difficulty = expiration * GlobalConfig.DIFF_MULTIPLIER;
            HashKey32 messageHash = HashKey32.Empty;
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

            Log.WriteConsole($"Heshed in {sw.ElapsedMilliseconds / 1000.0} seconds, {messageHash}");

            uint timestamp = (uint)DateTimeHelper.TimestampNow();

            //we append the creation timestamp after the PoW portion so we aren't burning the FTL window hashing the message
            finalMessage = messageHash
                .Concat(BitShifter.ToByte(timestamp))
                .Concat(finalMessage).ToArray();

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
    
        public static bool VerifyMessage(byte[] input, bool verifyFtl, out HashKey16 key, out IncomingMessage incomingMessage)
        {
            key = HashKey16.Make(input);
            incomingMessage = null;

            BinaryReader reader = new BinaryReader(new MemoryStream(input));
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            //take the message hash and timestamp as these are not part of the message that is hashed
            HashKey32 hash = input.Take(32).ToArray();
            uint timestamp = BitShifter.ToUInt(input.Skip(32).Take(4).ToArray());

            //Hash the rest of the message
            HashKey32 compare = SlowHash.Hash(input.Skip(36).ToArray());

            //compare the provided hash with the result of our own hashing
            if (hash != compare)
            {
                Log.WriteError($"Message failed validation. Incorrect hash, {hash} != {compare}");
                return false;
            }

            //extract nonce and expiration time
            uint nonce = BitShifter.ToUInt(input.Skip(36).Take(4).ToArray());
            uint expiration = BitShifter.ToUInt(input.Skip(40).Take(4).ToArray());

            //the hash matches, but is it valid for the provided expiration time?
            ulong difficulty = expiration * GlobalConfig.DIFF_MULTIPLIER;
            if (!Validator.CheckHash(compare, difficulty))
            {
                Log.WriteError($"Message failed validation. Invalid expiration time, {compare}");
                return false;
            }

            //FTL check, but only if we are verifying a message received through the ShareMessage p2p command
            //If we are receiving this via RequestMessagePool, we must skip this verification as we are pulling old
            //Messages and this check will in most cases fail
            if (verifyFtl && ((uint)Math.Abs(timestamp - (uint)DateTimeHelper.TimestampNow()) > GlobalConfig.FTL))
            {
                Log.WriteError($"Message failed validation. Outside future time limit");
                return false;
            }
            
            //Make sure expiration is the minimum
            if (expiration < GlobalConfig.MIN_MESSAGE_EXPIRATION)
            {
                Log.WriteError($"Message failed validation. Short life span");
                return false;
            }

            string address;
            HashKey16 readProofNonce;
            HashKey32 readProofHash;
            string message;
            KeyRing.DecryptMessage(input.Skip(44).ToArray(), out address, out readProofNonce, out readProofHash, out message);

            incomingMessage = new IncomingMessage(timestamp, expiration, address, message);
            incomingMessage.SetReadProof(readProofNonce, readProofHash);

            return true;
        }
    }
}
using System.Collections.Generic;
using AngryWasp.Helpers;
using EMS.Commands.P2P;
using System.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System;

namespace EMS
{
    public static class MessagePool
    {
        private static ConcurrentDictionary<HashKey16, Message> messages = new ConcurrentDictionary<HashKey16, Message>();
        public static ConcurrentDictionary<HashKey16, Message> Messages => messages;

        public static Message LastReceivedMessage { get; set; } = null;

        public static bool Send(string address, Message_Type messageType, byte[] message, uint expiration, out HashKey16 messageKey)
        {
            messageKey = HashKey16.Empty;

            if (message.Length > (16 * 1024))
            {
                Log.WriteError("Message exceeds the 16kb limit");
                return false;
            }

            byte[] encryptionResult, signature, addressXor;
            ReadProof readProof = ReadProof.Create();
    
            // Construct and encrypt the message
            //  - read proof nonce
            //  - message type
            //  - plain text message
            byte[] msg = new List<byte>(readProof.Nonce)
                .Join(new byte[] { (byte)messageType })
                .Join(message).ToArray();

            if (!KeyRing.EncryptMessage(msg, address, out encryptionResult, out signature, out addressXor))
                return false;

            // Construct the message that will be PoW hashed
            //  - nonce (empty uint until hashed)
            //  - message version (constant Config.MESSAGE_VERSION)
            //  - expiration time
            //  - encrypted message signature length
            //  - encrypted message length
            //  - obfuscated sender/receiver address
            //  - read proof hash
            //  - encrypted message signature
            //  - encrypted message
            byte[] finalMessage = new List<byte>(new byte[] { 0, 0, 0, 0, Config.MESSAGE_VERSION })
                .Join(BitShifter.ToByte(expiration))
                .Join(BitShifter.ToByte((ushort)signature.Length))
                .Join(BitShifter.ToByte((ushort)encryptionResult.Length))
                .Join(addressXor)
                .Join(readProof.Hash)
                .Join(signature)
                .Join(encryptionResult).ToArray();

            // Pow hash the message
            // We generate a new starting nonce and inser that into the first 4 bytes of the 
            // message, which we initialized with 0. We hash each nonce and increment until
            // we find a nonce that passes the difficulty target test
            uint x = MathHelper.Random.GenerateRandomSeed();
            ulong difficulty = Math.Max(expiration, Config.MIN_MESSAGE_EXPIRATION) * Config.DIFF_MULTIPLIER;
            HashKey32 messageHash = HashKey32.Empty;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while(true)
            {
                Buffer.BlockCopy(BitShifter.ToByte(x), 0, finalMessage, 0, 4);
                messageHash = SlowHash.Hash(finalMessage).ToByte();
                if (PowValidator.Validate(messageHash, difficulty))
                    break;

                ++x;
            }

            sw.Stop();

            Log.WriteConsole($"Hashed in {sw.ElapsedMilliseconds / 1000.0} seconds, {messageHash}");

            uint timestamp = (uint)DateTimeHelper.TimestampNow;

            // Finally we construct the final message
            //  - message hash (generated by the PoW hashing)
            //  - timestamp of the message
            //  - The rest of the message data
            // We append the creation timestamp after the PoW portion so 
            // we aren't burning the FTL window hashing the message
            finalMessage = messageHash.ToList()
                .Join(BitShifter.ToByte(timestamp))
                .Join(finalMessage).ToArray();

            messageKey = HashKey16.Make(finalMessage);

            // Add a new message to the pool.
            if (!messages.TryAdd(messageKey, new Message
            {
                Key = messageKey,
                Hash = messageHash,
                Data = finalMessage,
                Timestamp = timestamp,
                Expiration = expiration,
                MessageVersion = Config.MESSAGE_VERSION,
                MessageType = messageType,
                Address = address,
                DecryptedData = message,
                Direction = Message_Direction.Out,
                ReadProof = readProof
            }))
            { 
                Log.WriteWarning("Could not add message to the encrypted message pool. Message not sent"); 
                return false;
            }

            Helpers.MessageAll(ShareMessage.GenerateRequest(true, finalMessage).ToArray());

            return true;
        }

        public static bool MarkMessageRead(HashKey16 key)
        {
            Message msg = null;

            if (!messages.TryGetValue(key, out msg))
            {
                Log.WriteWarning($"Message with key {key} does not exist. Cannot mark as read");
                return false;
            }

            if (!msg.IsDecrypted)
            {
                Log.WriteWarning($"Message with key {key} is not decrypted. Cannot mark as read");
                return false;
            }

            if (msg.ReadProof == null)
            {
                Log.WriteWarning($"Message with key {key} does not have a valid read proof. Cannot mark as read");
                return false;
            }

            //mark the message read internally for anyone requesting the message pool later
            msg.ReadProof.IsRead = true;

            byte[] message = key.Concat(msg.ReadProof.Nonce).ToArray();
            byte[] req = ShareMessageRead.GenerateRequest(true, message).ToArray();

            Helpers.MessageAll(req);

            return true;
        }
    }
}
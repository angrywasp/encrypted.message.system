using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AngryWasp.Helpers;

namespace EMS
{
    public static class MessageValidator
    {
        public static Message Validate(this byte[] input, bool verifyFtl)
        {
            HashKey16 messageKey = HashKey16.Make(input);
            HashKey32 messageHash = HashKey32.Empty;
            uint timestamp = 0;
            uint expiration = 0;

            BinaryReader reader = new BinaryReader(new MemoryStream(input));
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            //read message hash
            messageHash = reader.ReadBytes(32);

            //read timestamp
            timestamp = reader.ReadUInt32();
            //skip nonce, 4 bytes
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            //read expiration time
            expiration = reader.ReadUInt32();

            reader.Close();

            //Hash the rest of the message
            HashKey32 compare = SlowHash.Hash(input.Skip(36).ToArray());

            //compare the provided hash with the result of our own hashing
            if (messageHash != compare)
            {
                Log.WriteError($"Message failed validation. Incorrect hash, {messageHash} != {compare}");
                return null;
            }

            //the hash matches, but is it valid for the provided expiration time?
            if (expiration < GlobalConfig.MIN_MESSAGE_EXPIRATION || !PowValidator.Validate(compare, expiration * GlobalConfig.DIFF_MULTIPLIER))
            {
                Log.WriteError($"Message failed validation. Invalid expiration time, {compare}");
                return null;
            }

            //FTL check, but only if we are verifying a message received through the ShareMessage p2p command
            //If we are receiving this via RequestMessagePool, we must skip this verification as we are pulling old
            //Messages and this check will in most cases fail
            if (verifyFtl && ((uint)Math.Abs(timestamp - (uint)DateTimeHelper.TimestampNow()) > GlobalConfig.FTL))
            {
                Log.WriteError($"Message failed validation. Outside future time limit");
                return null;
            }
            
            //Make sure expiration is the minimum
            if (expiration < GlobalConfig.MIN_MESSAGE_EXPIRATION)
            {
                Log.WriteError($"Message failed validation. Short life span");
                return null;
            }

            Message validatedMessage = new Message
            {
                Key = messageKey,
                Hash = messageHash,
                Data = input,
                Timestamp = timestamp,
                Expiration = expiration,
            };

            try
            {
                return KeyRing.DecryptMessage(validatedMessage.Data.Skip(44).ToArray(), validatedMessage);
            } 
            catch
            {
                return validatedMessage;
            }
        }
    }
}
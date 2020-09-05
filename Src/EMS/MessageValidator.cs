using System;
using System.IO;
using System.Linq;
using AngryWasp.Cli;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;

namespace EMS
{
    public static class MessageValidator
    {
        public static Message Validate(this byte[] input, bool verifyFtl)
        {
            HashKey16 messageKey = HashKey16.Make(input);
            HashKey32 messageHash = HashKey32.Empty;
            long timestamp = 0;
            uint expiration = 0;
            byte messageVersion = Config.MESSAGE_VERSION;

            BinaryReader reader = new BinaryReader(new MemoryStream(input));
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            //read message hash
            messageHash = reader.ReadBytes(32);

            //read timestamp
            timestamp = reader.ReadUInt32();
            //skip nonce, 4 bytes
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            //read message version
            messageVersion = reader.ReadByte();
            //read expiration time
            expiration = reader.ReadUInt32();

            reader.Close();

            //Hash the rest of the message
            HashKey32 compare = Keccak.Hash256(input.Skip(36).ToArray());

            //compare the provided hash with the result of our own hashing
            if (messageHash != compare)
            {
                Log.WriteError($"Message failed validation. Incorrect hash, {messageHash} != {compare}");
                return null;
            }

            if (messageVersion != Config.MESSAGE_VERSION)
                Log.WriteWarning($"Message has incorrect version {messageVersion}. Current version is {Config.MESSAGE_VERSION}");

            uint adjustedExpiration = Math.Max(expiration, Config.MIN_MESSAGE_EXPIRATION);

            //the hash matches, but is it valid for the provided expiration time?
            if (!PowValidator.Validate(compare, adjustedExpiration * Config.DIFF_MULTIPLIER))
            {
                Log.WriteError($"Message failed validation. Invalid expiration time, {adjustedExpiration}");
                return null;
            }

            //FTL check, but only if we are verifying a message received through the ShareMessage p2p command
            //If we are receiving this via RequestMessagePool, we must skip this verification as we are pulling old
            //Messages and this check will in most cases fail
            long localTimestamp = (long)DateTimeHelper.TimestampNow;
            uint variance = (uint)Math.Abs(timestamp - localTimestamp);
            if (verifyFtl && variance > Config.FTL)
            {
                Log.WriteError($"Message failed validation. Outside future time limit. {variance} > {Config.FTL}");
                Log.WriteError($"Message timestamp is {timestamp}, local timestamp is {localTimestamp}");
                return null;
            }
            
            //Make sure expiration is the minimum
            if (adjustedExpiration < Config.MIN_MESSAGE_EXPIRATION)
            {
                Log.WriteError($"Message failed validation. Short life span");
                return null;
            }

            Message validatedMessage = new Message
            {
                Key = messageKey,
                Hash = messageHash,
                Data = input,
                Timestamp = (uint)timestamp,
                Expiration = expiration
            };

            try
            {
                return KeyRing.DecryptMessage(validatedMessage.Data.Skip(45).ToArray(), validatedMessage);
            } 
            catch
            {
                return validatedMessage;
            }
        }
    }
}
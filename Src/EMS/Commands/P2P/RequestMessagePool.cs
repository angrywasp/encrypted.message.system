
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngryWasp.Helpers;
using AngryWasp.Net;

namespace EMS.Commands.P2P
{
    public class RequestMessagePool
    {
        public const byte CODE = 12;

        public static List<byte> GenerateRequest(bool isRequest) 
        {
            // We construct a message that lists all of the message hashes we have
            // along with a byte flag that indicates if this message is read

            List<byte> message = new List<byte>();
            foreach (var m in MessagePool.Messages)
            {
                message.AddRange(m.Key);
                message.Add(m.Value.ReadProof.IsRead ? (byte)1 : (byte)0);
            }

            return Header.Create(CODE, isRequest, (ushort)(MessagePool.Messages.Count * 17))
                .Join(message);
        }

        public static void GenerateResponse(Connection c, Header h, byte[] d)
        {
            if (h.IsRequest)
            {
                //reconstruct the message data to a list of keys to compare with
                Dictionary<HashKey16, byte> hashes = new Dictionary<HashKey16, byte>();

                if (d.Length > 0)
                {
                    BinaryReader reader = new BinaryReader(new MemoryStream(d));

                    int count = d.Length / 17;
                    for (int i = 0; i < count; i++)
                        hashes.Add(reader.ReadBytes(16), reader.ReadByte());

                    reader.Close();
                }

                // The max message length of the p2p protocol is ushort.MaxValue
                // So we iterate all the messages in our local pool and send them
                // Splitting the response into multiple messages if required

                List<byte> payload = new List<byte>();

                foreach (var m in MessagePool.Messages)
                {
                    bool sendPruned = true;

                    if (hashes.ContainsKey(m.Key))
                    {
                        byte comp = m.Value.ReadProof.IsRead ? (byte)1 : (byte)0;

                        // Skip. Read status matches or the incoming data says this message is read
                        if (hashes[m.Key] == comp || hashes[m.Key] == 1)
                            continue;
                    }
                    else
                        sendPruned = false; // Requesting node does not have this message. Send full

                    HashKey16 readProofNonce = HashKey16.Empty;
                    if (m.Value.ReadProof.IsRead)
                        readProofNonce = m.Value.ReadProof.Nonce;

                    List<byte> entry = m.Key.ToList().Join(readProofNonce);

                    // If send pruned is true, it means that the requesting node already has this message
                    // So all we want to send is the message key and the read proof nonce and the requesting node
                    // Can use their local data to validate the nonce

                    if (sendPruned)
                        entry = entry.Join(BitShifter.ToByte((ushort)0));
                    else
                        entry = entry
                            .Join(BitShifter.ToByte((ushort)m.Value.Data.Length))
                            .Join(m.Value.Data);

                    // Adding this entry would place the message length above the maximum
                    // So we send what we have and then start a new message
                    if (payload.Count + entry.Count > ushort.MaxValue)
                    {
                        List<byte> data = Header.Create(CODE, false, (ushort)payload.Count)
                            .Join(payload);

                        if (!c.Write(data.ToArray()))
                        {
                            ConnectionManager.Remove(c);
                            return;
                        }

                        payload.Clear();
                        payload.AddRange(entry);
                    }
                    else
                        payload.AddRange(entry);
                }

                // Send any remaining data
                if (payload.Count > 0)
                {
                    List<byte> data = Header.Create(CODE, false, (ushort)payload.Count)
                        .Join(payload);

                    if (!c.Write(data.ToArray()))
                    {
                        ConnectionManager.Remove(c);
                        return;
                    }
                }
            }
            else
            {
                int bytesRead = 0;
                int newMessageCount = 0;
                BinaryReader reader = new BinaryReader(new MemoryStream(d));

                while (true)
                {
                    if (bytesRead >= h.DataLength)
                        break;

                    HashKey16 messageKey = reader.ReadBytes(16);
                    HashKey16 readProofNonce = reader.ReadBytes(16);
                    
                    ushort messageLength = BitShifter.ToUShort(reader.ReadBytes(2));
                    byte[] messageBody = null;

                    if (messageLength > 0)
                        messageBody = reader.ReadBytes(messageLength);

                    bytesRead += (34 + messageLength);

                    Message msg = null;
                    if (!MessagePool.Messages.TryGetValue(messageKey, out msg))
                    {
                        // This message is not in our local pool.
                        if (messageBody == null || messageBody.Length != messageLength)
                        {
                            // We were not provided with a full message
                            c.AddFailure();
                            continue; 
                        }

                        msg = messageBody.Validate(false);

                        if (msg == null)
                        {
                            // Message failed validation
                            c.AddFailure();
                            continue;
                        }

                        if (msg.Key != messageKey)
                        {
                            //The calculated key does not match the provided key
                            Log.WriteError($"Message key mismatch {msg.Key} != {messageKey}");
                            c.AddFailure();
                            continue; 
                        }

                        if (!MessagePool.Messages.TryAdd(messageKey, msg))
                            Log.WriteError($"Could not add message to the pool");
                    }

                    ++newMessageCount;
                    if (msg.IsDecrypted)
                        Log.WriteConsole($"Received a message with key {msg.Key}");
                    
                    //validate the read proof and update if necessary
                    if (readProofNonce != HashKey16.Empty)
                    {
                        HashKey32 readProofHash = ReadProof.GenerateHash(readProofNonce);
                        HashKey32 compareReadProofHash = msg.ExtractReadProofHash();

                        if (readProofHash != compareReadProofHash)
                        {
                            Log.WriteError($"Read proof for message {msg.Key} failed validation");
                            c.AddFailure();
                            continue;
                        }

                        // Read proof passed verification. Add it to the message
                        msg.ReadProof = new ReadProof
                        {
                            Nonce = readProofNonce,
                            Hash = readProofHash,
                            IsRead = true
                        };
                    }
                }

                Log.WriteConsole($"{newMessageCount} new messages added to the pool");

                reader.Close();
            }
        }
    }
}
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

        public static byte[] GenerateRequest(bool isRequest) 
        {
            List<byte> message = new List<byte>();

            //we inform the other nodes of the messages we have and they will send back the ones we don't
            message.AddRange(Header.Create(CODE, isRequest, (ushort)(MessagePool.EncryptedMessages.Count * 17)));
            foreach (var m in MessagePool.EncryptedMessages)
            {
                message.AddRange(m.Key);
                message.Add(m.Value.HasReadProof() ? (byte)1 : (byte)0);
            }

            return message.ToArray();
        }

        public static void GenerateResponse(Connection c, Header h, byte[] d)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(d));

            if (h.IsRequest)
            {
                //reconstruct the hashkey list to compare our local message pool to
                Dictionary<HashKey16, byte> hashes = new Dictionary<HashKey16, byte>();

                if (d.Length > 0)
                {
                    //request includes a list of the messages already in the requesting nodes pool
                    //rebuild this list
                    int count = d.Length / 17;
                    for (int i = 0; i < count; i++)
                        hashes.Add(reader.ReadBytes(16), reader.ReadByte());
                }

                //the max message length of the p2p protocol is ushort.MaxValue
                //so we first construct a list of each message we want to send and then pack them into 
                //multiple response buffers

                List<byte> payload = new List<byte>();

                foreach (var m in MessagePool.EncryptedMessages)
                {
                    //response format
                    //message key 
                    //read proof nonce (16 bytes, 0 if null)
                    //message length (ushort)
                    //message bytes (variable n bytes of length)\
                    List<byte> msgBytes = new List<byte>();
                    HashKey16 readProofKey;
                    HashKey32 readProofHash;
                    bool hasReadProof = m.Value.GetReadProof(out readProofKey, out readProofHash);

                    //if the requesting node already has this message
                    //we only send an read proof if the requesting node doesn't have it and we do
                    if (hashes.ContainsKey(m.Key))
                    {
                        if (hashes[m.Key] == 0 && hasReadProof)
                        {
                            //Send pruned
                            //this list will be verified against the messages in the local message pool
                            msgBytes.AddRange(m.Key);
                            msgBytes.AddRange(readProofKey);
                            msgBytes.AddRange(BitShifter.ToByte((ushort)0));
                        }
                        else
                            continue;
                    }
                    else
                    {
                        //Send full message
                        msgBytes.AddRange(m.Key);
                        msgBytes.AddRange(readProofKey);
                        msgBytes.AddRange(BitShifter.ToByte((ushort)m.Value.Message.Length));
                        msgBytes.AddRange(m.Value.Message);
                    }

                    if (payload.Count + msgBytes.Count < ushort.MaxValue)
                        payload.AddRange(msgBytes);
                    else
                    {
                        //send what we have then add this entry after to a new payload
                        List<byte> data = Header.Create(CODE, false, (ushort)payload.Count).ToList();
                        data.AddRange(payload);
                        c.Write(data.ToArray());

                        payload.Clear();
                        payload.AddRange(msgBytes);
                    }

                    if (payload.Count > 0)
                    {
                        List<byte> data = Header.Create(CODE, false, (ushort)payload.Count).ToList();
                        data.AddRange(payload);
                        c.Write(data.ToArray());
                    }
                }
            }
            else
            {
                //todo: parse response into local message pool
                int bytesRead = 0;

                while (true)
                {
                    if (bytesRead >= h.DataLength)
                        break;

                    HashKey16 key = reader.ReadBytes(16);
                    HashKey16 readProofKey = reader.ReadBytes(16);
                    
                    ushort length = BitShifter.ToUShort(reader.ReadBytes(2));
                    byte[] msg = null;

                    if (length > 0)
                        msg = reader.ReadBytes(length);

                    bytesRead += (34 + length);

                    EncryptedMessage e = null;
                    MessagePool.EncryptedMessages.TryGetValue(key, out e);

                    if (e == null) //this message is not in our local pool
                    {
                        if (msg == null)
                            continue; //we were not provided with a full message. skip
                        
                        e = new EncryptedMessage(msg);
                        if (!MessagePool.EncryptedMessages.TryAdd(key, e))
                            continue; //couldn't add the encrypted message pool. skip

                        //generate and verify the proof. If it doesn't match, add a failure
                        if (!readProofKey.IsNullOrEmpty())
                        {
                            HashKey32 readProofHash = ProvableMessage.GenerateHash(readProofKey);
                            if (readProofHash == e.ExtractReadProofHash())
                                e.SetReadProof(readProofKey, readProofHash);
                            else
                                c.AddFailure();
                        }
                        
                        //Check if this message is incoming to us. 
                        IncomingMessage i;
                        if (!KeyRing.DecryptMessage(msg, out i) || !MessagePool.IncomingMessages.TryAdd(key, i))
                            continue;

                        //Mark as read if the accompanying encrypted message has the proof.
                        i.SetAsRead(e.HasReadProof());
                    }
                    else //we have this in our local message pool
                    {
                        if (!readProofKey.IsNullOrEmpty())
                        {
                            HashKey32 readProofHash = ProvableMessage.GenerateHash(readProofKey);
                            if (readProofHash == e.ExtractReadProofHash())
                                e.SetReadProof(readProofKey, readProofHash);
                            else
                                c.AddFailure();
                        }
                    }
                }
            }
        }
    }
}
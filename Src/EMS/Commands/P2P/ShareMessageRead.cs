using System.Collections.Generic;
using System.Linq;
using AngryWasp.Net;

namespace EMS.Commands.P2P
{
    public class ShareMessageRead
    {
        public const byte CODE = 13;

        public static byte[] GenerateRequest(bool isRequest, byte[] message)
        {
            List<byte> data = new List<byte>();
            data.AddRange(Header.Create(CODE, isRequest, (ushort)(message.Length)));
            data.AddRange(message);
            return data.ToArray();
        }

        public static void GenerateResponse(Connection c, Header h, byte[] d)
        {
            HashKey16 key = d.Take(16).ToArray();
            HashKey16 readProofKey = d.Skip(16).Take(16).ToArray();

            EncryptedMessage e = null;
            if (!MessagePool.EncryptedMessages.TryGetValue(key, out e))
            {
                Log.WriteWarning($"{CommandCode.CommandString(CODE)}: Verification failed. Orphan");
                c.AddFailure();
                return;
            }

            HashKey32 readProofHash = ProvableMessage.GenerateHash(readProofKey);
            
            if (readProofHash != e.ExtractReadProofHash())
            {
                Log.WriteError($"{CommandCode.CommandString(CODE)}: Verification failed. Mismatched hash");
                c.AddFailure();
                return;
            }

            e.SetReadProof(readProofKey, readProofHash);

            byte[] req = ShareMessageRead.GenerateRequest(true, d);

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (con) =>
            {
                if (con.PeerId == h.PeerID)
                    return; //don't return to sender

                con.Write(req);
            });
        }
    }
}
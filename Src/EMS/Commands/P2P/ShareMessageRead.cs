using AngryWasp.Cli;
using AngryWasp.Helpers;
using AngryWasp.Net;
using System.Collections.Generic;
using System.Linq;

namespace EMS.Commands.P2P
{
    public class ShareMessageRead
    {
        public const byte CODE = 13;

        public static List<byte> GenerateRequest(bool isRequest, byte[] message)
        {
            return Header.Create(CODE, isRequest, (ushort)(message.Length))
                .Join(message);
        }

        public static void GenerateResponse(Connection c, Header h, byte[] d)
        {
            HashKey16 messageKey = d.Take(16).ToArray();
            HashKey16 readProofNonce = d.Skip(16).Take(16).ToArray();

            Message msg = null;
            if (!MessagePool.Messages.TryGetValue(messageKey, out msg))
            {
                Log.WriteWarning($"{CommandCode.CommandString(CODE)}: Verification failed. Orphan");
                c.AddFailure();
                return;
            }

            HashKey32 readProofHash = ReadProof.GenerateHash(readProofNonce);
            
            if (readProofHash != msg.ExtractReadProofHash())
            {
                Log.WriteError($"{CommandCode.CommandString(CODE)}: Verification failed. Mismatched hash");
                c.AddFailure();
                return;
            }

            msg.ReadProof = new ReadProof
            {
                Nonce = readProofNonce,
                Hash = readProofHash,
                IsRead = true
            };

            byte[] req = ShareMessageRead.GenerateRequest(true, d).ToArray();

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (con) =>
            {
                if (con.PeerId == h.PeerID)
                    return; //don't return to sender

                con.Write(req);
            });
        }
    }
}
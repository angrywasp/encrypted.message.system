using System.Collections.Generic;
using AngryWasp.Helpers;
using AngryWasp.Net;

namespace EMS.Commands.P2P
{
    public class ShareMessage
    {
        public const byte CODE = 11;

        public static byte[] GenerateRequest(bool isRequest, ulong timestamp, byte[] message)
        {
            List<byte> data = new List<byte>();
            data.AddRange(Header.Create(CODE, isRequest, (ushort)(message.Length + 8)));
            data.AddRange(BitShifter.ToByte(timestamp));
            data.AddRange(message);
            return data.ToArray();
        }

        public static byte[] GenerateRequest(bool isRequest, byte[] message)
        {
            List<byte> data = new List<byte>();
            data.AddRange(Header.Create(CODE, isRequest, (ushort)(message.Length + 8)));
            data.AddRange(message);
            return data.ToArray();
        }

        public static void GenerateResponse(Connection c, Header h, byte[] d)
        {
            MessagePool.Add(d);

            //we still proceed with propagating the message so anyone attempting to trace the message will not see it stop here
            //and assume this is the intended destination. Messages will self destruct after a time anyway

            //replace the header with a new one and forward it
            byte[] req = ShareMessage.GenerateRequest(true, d);

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (con) =>
            {
                if (con.PeerId == h.PeerID)
                    return; //don't return to sender

                con.Write(req);
            });
        }
    }
}
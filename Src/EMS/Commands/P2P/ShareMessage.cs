using System.Collections.Generic;
using AngryWasp.Cryptography;
using AngryWasp.Net;

namespace EMS.Commands.P2P
{
    public class ShareMessage
    {
        public const byte CODE = 11;

        public static byte[] GenerateRequest(bool isRequest, byte[] message)
        {
            List<byte> data = new List<byte>();
            data.AddRange(Header.Create(CODE, isRequest, (ushort)(message.Length)));
            data.AddRange(message);
            return data.ToArray();
        }

        public static void GenerateResponse(Connection c, Header h, byte[] d)
        {
            HashKey16 key = HashKey16.Make(d);

            //check if this message is for us
            IncomingMessage i;
            if (KeyRing.DecryptMessage(d, out i) && MessagePool.IncomingMessages.TryAdd(key, i))
                Log.WriteConsole($"Received a message with key {key}");

            //Add to the encrypted message pool
            if (MessagePool.EncryptedMessages.ContainsKey(key))
                return; //already have it. skip. cause we already would have shared this the first time we got it

            //Try adding it. If this doesn't work, we still forward the message anyway and we'll try again if the message comes back or a timed sync
            MessagePool.EncryptedMessages.TryAdd(key, new EncryptedMessage(d));

            byte[] req = ShareMessage.GenerateRequest(true, d);

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (con) =>
            {
                if (con.PeerId == h.PeerID)
                    return;

                con.Write(req);
            });
        }
    }
}
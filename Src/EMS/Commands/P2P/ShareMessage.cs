using System.Collections.Generic;
using AngryWasp.Net;
using AngryWasp.Helpers;

namespace EMS.Commands.P2P
{
    public class ShareMessage
    {
        public const byte CODE = 11;

        public static List<byte> GenerateRequest(bool isRequest, byte[] message)
        {
            return Header.Create(CODE, isRequest, (ushort)(message.Length))
                .Join(message);
        }

        public static void GenerateResponse(Connection c, Header h, byte[] d)
        {
            Message msg = d.Validate(true);

            if (msg == null)
            {
                c.AddFailure();
                return;
            }

            if (MessagePool.Messages.TryAdd(msg.Key, msg))
            {
                if (msg.IsDecrypted)
                {
                    Log.WriteConsole($"Received a message with key {msg.Key}");
                    MessagePool.LastReceivedMessage = msg;
                }
            }
            else
                //already have it. skip. cause we already would have shared this the first time we got it
                return; 

            byte[] request = ShareMessage.GenerateRequest(true, d).ToArray();

            List<Connection> disconnected = new List<Connection>();

            ConnectionManager.ForEach(Direction.Incoming | Direction.Outgoing, (con) =>
            {
                if (con.PeerId == h.PeerID)
                    return;

                con.Write(request);
            });

            foreach (var disc in disconnected)
                ConnectionManager.Remove(disc);
        }
    }
}
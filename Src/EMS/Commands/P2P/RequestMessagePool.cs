using System.Collections.Generic;
using System.IO;
using AngryWasp.Net;

namespace EMS.Commands.P2P
{
    public class RequestMessagePool
    {
        public const byte CODE = 12;

        public static byte[] GenerateRequest(bool isRequest) 
        {
            //we inform the other nodes of the messages we have and they will send back the ones we don't
            byte[] header = Header.Create(CODE, isRequest, (ushort)(MessagePool.Count * 16));

            List<byte> bytes = new List<byte>();
            bytes.AddRange(header);
            foreach (var m in MessagePool.Messages)
                bytes.AddRange(m.Key);

            return bytes.ToArray();
        }

        public static byte[] GenerateRequest(bool isRequest, List<byte> data) 
        {
            //we inform the other nodes of the messages we have and they will send back the ones we don't
            byte[] header = Header.Create(CODE, isRequest, (ushort)data.Count);

            List<byte> bytes = new List<byte>();
            bytes.AddRange(header);
            bytes.AddRange(data);

            return bytes.ToArray();
        }

        public static void GenerateResponse(Connection c, Header h, byte[] d)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(d));
            
            if (d.Length > 0)
            {
                int count = d.Length / 16;
                HashSet<HashKey> hashes = new HashSet<HashKey>();

                for (int i = 0; i < count; i++)
                    hashes.Add(reader.ReadBytes(16));

                List<byte> bytes = new List<byte>();
                foreach (var m in MessagePool.Messages)
                    if (!hashes.Contains(m.Key))
                        bytes.AddRange(m.Value.Message);

                if (bytes.Count == 0)
                    return;

                // if isRequest == true, we are on the server so we reply with the messages the client is missing
                //we could check here if the client has any messages we don't, but we are continuously requesting messages from each other, so forget it
                if (h.IsRequest)
                    c.Write(GenerateRequest(false, bytes));

                return;
            }

            if (h.IsRequest)
                c.Write(GenerateRequest(false));
        }
    }
}
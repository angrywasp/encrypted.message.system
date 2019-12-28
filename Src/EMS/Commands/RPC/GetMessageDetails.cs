using Newtonsoft.Json;
using System.Collections.Generic;

namespace EMS.Commands.RPC
{
    public class GetMessageDetails
    {
        public static bool Handle(object json, out object jsonResult)
        {
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
            ret.Response = new JsonResponse();

            foreach (var m in MessagePool.EncryptedMessages)
                ret.Response.Encrypted.Add(new EncryptedMessage
                {
                    Hash = m.Key.ToString()
                });
            
            foreach (var m in MessagePool.IncomingMessages)
                ret.Response.Incoming.Add(new IncomingMessage
                {
                    Hash = m.Key.ToString(),
                    Sender = m.Value.Sender
                });

            foreach (var m in MessagePool.OutgoingMessages)
                ret.Response.Outgoing.Add(new OutgoingMessage
                {
                    Hash = m.Key.ToString(),
                    Recipient = m.Value.Recipient
                });

            jsonResult = ret;
            
            return true;
        }

        public class EncryptedMessage
        {
            [JsonProperty("hash")]
            public string Hash { get; set; }
        }

        public class IncomingMessage : EncryptedMessage
        {
            [JsonProperty("sender")]
            public string Sender { get; set; }
        }

        public class OutgoingMessage : EncryptedMessage
        {
            [JsonProperty("recipient")]
            public string Recipient { get; set; }
        }

        public class JsonResponse
        {
            [JsonProperty("encrypted")]
            public List<EncryptedMessage> Encrypted { get; set; } = new List<EncryptedMessage>();

            [JsonProperty("incoming")]
            public List<IncomingMessage> Incoming { get; set; } = new List<IncomingMessage>();

            [JsonProperty("outgoing")]
            public List<OutgoingMessage> Outgoing { get; set; } = new List<OutgoingMessage>();
        }
    }
}
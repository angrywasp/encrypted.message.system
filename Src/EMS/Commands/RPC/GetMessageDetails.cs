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

            foreach (var m in MessagePool.Messages)
                ret.Response.Encrypted.Add(new EncryptedMessageItem
                {
                    Hash = m.Key,
                    Timestamp = m.Value.TimeStamp
                });
            
            foreach (var m in MessagePool.DecryptedMessages)
                ret.Response.Decrypted.Add(new DecryptedMessageItem
                {
                    Hash = m.Key,
                    Timestamp = m.Value.TimeStamp,
                    Sender = m.Value.Sender
                });

            jsonResult = ret;
            
            return true;
        }

        public class EncryptedMessageItem
        {
            [JsonProperty("hash")]
            public HashKey Hash { get; set; }

            [JsonProperty("timestamp")]
            public ulong Timestamp { get; set; }
        }

        public class DecryptedMessageItem : EncryptedMessageItem
        {
            [JsonProperty("sender")]
            public string Sender { get; set; }
        }

        public class JsonResponse
        {
            [JsonProperty("encrypted")]
            public List<EncryptedMessageItem> Encrypted { get; set; } = new List<EncryptedMessageItem>();

            [JsonProperty("decrypted")]
            public List<DecryptedMessageItem> Decrypted { get; set; } = new List<DecryptedMessageItem>();
        }
    }
}
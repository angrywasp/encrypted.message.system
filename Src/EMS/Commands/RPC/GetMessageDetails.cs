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
            {
                bool isRead = m.Value.ReadProof != null && m.Value.ReadProof.IsRead;
                string direction = m.Value.IsDecrypted ? (MessagePool.OutgoingMessages.Contains(m.Key) ? "out": "in") : string.Empty;

                ret.Response.Details.Add(new MessageDetail
                {
                    Key = m.Key,
                    Timestamp = m.Value.Timestamp,
                    Expiration = m.Value.Expiration,
                    Address = m.Value.Address,
                    Read = isRead,
                    Direction = direction
                });
            }
            
            jsonResult = ret;
            
            return true;
        }

        public class MessageDetail
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;

            [JsonProperty("timestamp")]
            public uint Timestamp { get; set; } = 0;

            [JsonProperty("expiration")]
            public uint Expiration { get; set; } = 0;

            [JsonProperty("address")]
            public string Address { get; set; } = string.Empty;

            [JsonProperty("read")]
            public bool Read { get; set; } = false;
            
            [JsonProperty("direction")]
            public string Direction { get; set; } = string.Empty;
        }

        public class JsonResponse
        {
            [JsonProperty("details")]
            public List<MessageDetail> Details { get; set; } = new List<MessageDetail>();
        }
    }
}
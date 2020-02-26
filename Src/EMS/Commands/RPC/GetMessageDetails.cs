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

                ret.Response.Details.Add(new MessageDetail
                {
                    Key = m.Key,
                    Timestamp = m.Value.Timestamp,
                    Expiration = m.Value.Expiration,
                    Address = m.Value.Address,
                    Read = isRead,
                    Direction = m.Value.Direction.ToString().ToLower(),
                    MessageVersion = m.Value.MessageVersion,
                    MessageType = m.Value.MessageType                
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

            [JsonProperty("version")]
            public byte MessageVersion { get; set; } = 0;

            [JsonProperty("type")]
            public Message_Type MessageType { get; set; } = Message_Type.Invalid;

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
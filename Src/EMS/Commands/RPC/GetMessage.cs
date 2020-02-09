using Newtonsoft.Json;
using AngryWasp.Helpers;
using System;
using System.Text;

namespace EMS.Commands.RPC
{
    public class GetMessage
    {
        public static bool Handle(object json, out object jsonResult)
        {
            EMS.JsonRequest<JsonRequest> r = (EMS.JsonRequest<JsonRequest>)json;
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
            ret.Response = new JsonResponse();

            HashKey16 key = r.Request.Key;

            Message message;

            if (!MessagePool.Messages.TryGetValue(key, out message))
            {
                jsonResult = ret;
                return false;
            }

            ret.Response.Key = message.Key;
            ret.Response.Hash = message.Hash;
            ret.Response.Timestamp = message.Timestamp;
            ret.Response.Expiration = message.Expiration;

            ret.Response.Direction = message.IsDecrypted ? (MessagePool.OutgoingMessages.Contains(message.Key) ? "out": "in") : string.Empty;

            if (message.ReadProof != null)
                ret.Response.ReadProof = message.ReadProof;

            //don't fill message.Data. It is a waste of bandwidth

            if (!message.IsDecrypted)
            {
                jsonResult = ret;
                return true;
            }

            ret.Response.Address = message.Address;
            ret.Response.Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(message.DecryptedMessage));

            jsonResult = ret;
            return true;
        }

        public class JsonRequest
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; }
        }

        public class JsonResponse
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;

            [JsonProperty("hash")]
            public HashKey32 Hash { get; set; } = HashKey32.Empty;

            [JsonProperty("timestamp")]
            public uint Timestamp { get; set; } = 0;

            [JsonProperty("expiration")]
            public uint Expiration { get; set; } = 0;

            [JsonProperty("address")]
            public string Address { get; set; } = string.Empty;

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;

            [JsonProperty("direction")]
            public string Direction { get; set; } = string.Empty;

            [JsonProperty("read_proof")]
            public ReadProof ReadProof { get; set; } = new ReadProof();
        }
    }
}
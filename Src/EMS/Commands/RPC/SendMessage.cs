using Newtonsoft.Json;
using System;

namespace EMS.Commands.RPC
{
    public class SendMessage
    {
        public static bool Handle(object json, out object jsonResult)
        {
            EMS.JsonRequest<JsonRequest> r = (EMS.JsonRequest<JsonRequest>)json;
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
            ret.Response = new JsonResponse();

            byte[] messageBytes = Convert.FromBase64String(r.Request.Message);

            HashKey16 key;
            bool sent = MessagePool.Send(r.Request.Address, Message_Type.Text, messageBytes, r.Request.Expiration, out key);
            if (sent)
                ret.Response.Key = key;

            jsonResult = ret;
            return sent;
        }

        public class JsonRequest
        {
            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("expiration")]
            public uint Expiration { get; set; } = 3600;
        }

        public class JsonResponse
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;
        }
    }
}
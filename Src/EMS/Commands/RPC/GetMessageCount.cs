using Newtonsoft.Json;

namespace EMS.Commands.RPC
{
    public class GetMessageCount
    {
        public static bool Handle(object json, out object jsonResult)
        {
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
            ret.Response = new JsonResponse
            {
                Encrypted = MessagePool.EncryptedMessages.Count,
                Incoming = MessagePool.IncomingMessages.Count,
                Outgoing = MessagePool.OutgoingMessages.Count
            };

            jsonResult = ret;
            
            return true;
        }

        public class JsonResponse
        {
            [JsonProperty("encrypted")]
            public int Encrypted { get; set; }

            [JsonProperty("incoming")]
            public int Incoming { get; set; }

            [JsonProperty("outgoing")]
            public int Outgoing { get; set; }
        }
    }
}
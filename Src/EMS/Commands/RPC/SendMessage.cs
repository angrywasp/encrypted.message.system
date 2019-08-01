using Newtonsoft.Json;

namespace EMS.Commands.RPC
{
    public class SendMessage
    {
        public static bool Handle(object json, out object jsonResult)
        {
            EMS.JsonRequest<JsonRequest> r = (EMS.JsonRequest<JsonRequest>)json;
            HashKey hk = MessagePool.Send(r.Request.Address, r.Request.Message);

            if (hk == HashKey.Empty)
            {
                jsonResult = new EMS.JsonResponse<string>
                {
                    Response = "Message with calculated key already exists"
                };
                return false;
            }
            else
            {
                EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
                ret.Response.Hash = hk.ToString();
                jsonResult = ret;
                return true;
            }
        }

        public class JsonRequest
        {
            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }

        public class JsonResponse
        {
            [JsonProperty("hash")]
            public string Hash { get; set; }
        }
    }
}
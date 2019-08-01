using Newtonsoft.Json;
using AngryWasp.Helpers;

namespace EMS.Commands.RPC
{
    public class GetMessageCount
    {
        public static bool Handle(object json, out object jsonResult)
        {
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
            ret.Response = new JsonResponse
            {
                Total = MessagePool.Count,
                Decrypted = MessagePool.DecryptedCount
            };

            jsonResult = ret;
            
            return true;
        }

        public class JsonResponse
        {
            [JsonProperty("total")]
            public int Total { get; set; }

            [JsonProperty("decrypted")]
            public int Decrypted { get; set; }
        }
    }
}
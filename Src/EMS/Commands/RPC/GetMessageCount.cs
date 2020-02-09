using Newtonsoft.Json;

namespace EMS.Commands.RPC
{
    public class GetMessageCount
    {
        public static bool Handle(object json, out object jsonResult)
        {
            int total = MessagePool.Messages.Count;
            int decrypted = 0;

            foreach (var m in MessagePool.Messages)
            {
                if (m.Value.IsDecrypted)
                    ++decrypted;
            }
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
            ret.Response = new JsonResponse
            {
                Total = total,
                Decrypted = decrypted
            };

            jsonResult = ret;
            
            return true;
        }

        public class JsonResponse
        {
            [JsonProperty("total")]
            public int Total { get; set; } = 0;

            [JsonProperty("decrypted")]
            public int Decrypted { get; set; } = 0;
        }
    }
}
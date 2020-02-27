using Newtonsoft.Json;
using AngryWasp.Helpers;

namespace EMS.Commands.RPC
{
    public class GetAddress
    {
        public static bool Handle(object json, out object jsonResult)
        {
            if (Config.User.RelayOnly)
            {
                var j = new JsonResponseBase();
                j.ErrorCode = 100;
                jsonResult = j;
                return false;
            }

            EMS.JsonRequest<JsonRequest> r = (EMS.JsonRequest<JsonRequest>)json;
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();

            ret.Response = new JsonResponse
            {
                PublicKey= Base58.Encode(KeyRing.PublicKey),
                PublicKeyHex = KeyRing.PublicKey.ToHex()
            };

            if (r.Request.Private)
            {
                ret.Response.PrivateKey = Base58.Encode(KeyRing.PrivateKey);
                ret.Response.PrivateKeyHex = KeyRing.PrivateKey.ToHex();
            }

            jsonResult = ret;

            return true;
        }

        public class JsonRequest
        {
            [JsonProperty("private")]
            public bool Private { get; set; } = false;
        }

        public class JsonResponse
        {
            [JsonProperty("public")]
            public string PublicKey { get; set; } = string.Empty;

            [JsonProperty("public_hex")]
            public string PublicKeyHex { get; set; } = string.Empty;

            [JsonProperty("private")]
            public string PrivateKey { get; set; } = string.Empty;

            [JsonProperty("private_hex")]
            public string PrivateKeyHex { get; set; } = string.Empty;
        }
    }
}
using Newtonsoft.Json;
using AngryWasp.Helpers;

namespace EMS.Commands.RPC
{
    public class GetAddress
    {
        public static bool Handle(object json, out object jsonResult)
        {
            EMS.JsonRequest<JsonRequest> r = (EMS.JsonRequest<JsonRequest>)json;
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
            ret.Response = new JsonResponse
            {
                PublicKeyBase58 = Base58.Encode(KeyRing.PublicKey),
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
            public bool Private { get; set; }
        }

        public class JsonResponse
        {
            [JsonProperty("public")]
            public string PublicKeyBase58 { get; set; }

            [JsonProperty("public_hex")]
            public string PublicKeyHex { get; set; }

            [JsonProperty("private")]
            public string PrivateKey { get; set; }

            [JsonProperty("private_hex")]
            public string PrivateKeyHex { get; set; }
        }
    }
}
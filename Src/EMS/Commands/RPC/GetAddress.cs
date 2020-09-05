using Newtonsoft.Json;
using AngryWasp.Helpers;
using AngryWasp.Json.Rpc;

namespace EMS.Commands.Rpc
{
    [JsonRpcServerCommand("get_address")]
    public class GetAddress : IJsonRpcServerCommand
    {
        public bool Handle(string requestString, out object responseObject)
        {
            JsonRequest<Request> request = null;
            responseObject = null;

            if (Config.User.RelayOnly)
            {
                responseObject  = new JsonResponseBase() {
                    ErrorCode = 100
                };

                return false;
            }

            if (!JsonRequest<Request>.Deserialize(requestString, out request))
                return false;

            JsonResponse<Response> response = new JsonResponse<Response>();
            response.Data.PublicKey = Base58.Encode(KeyRing.PublicKey);
            response.Data.PublicKeyHex = KeyRing.PublicKey.ToHex();

            if (request.Data.Private)
            {
                response.Data.PrivateKey = Base58.Encode(KeyRing.PrivateKey);
                response.Data.PrivateKeyHex = KeyRing.PrivateKey.ToHex();
            }

            responseObject = response;
            return true;
        }

        public class Request
        {
            [JsonProperty("private")]
            public bool Private { get; set; } = false;
        }

        public class Response
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
using AngryWasp.Json.Rpc;
using Newtonsoft.Json;
using System.Linq;

namespace EMS.Commands.RPC
{
    [JsonRpcServerCommand("get_message_count")]
    public class GetMessageCount : IJsonRpcServerCommand
    {
        public bool Handle(string requestString, out object responseObject)
        {
            int total = MessagePool.Messages.Count;
            int decrypted = MessagePool.Messages.Where(x => x.Value.IsDecrypted).Count();

            JsonResponse<Response> response = new JsonResponse<Response>();
            response.Data.Total = total;
            response.Data.Decrypted = decrypted;

            responseObject = response;
            return true;
        }

        public class Response
        {
            [JsonProperty("total")]
            public int Total { get; set; } = 0;

            [JsonProperty("decrypted")]
            public int Decrypted { get; set; } = 0;
        }
    }
}
using AngryWasp.Json.Rpc;
using Newtonsoft.Json;
using System;

namespace EMS.Commands.RPC
{
    [JsonRpcServerCommand("send_message")]
    public class SendMessage : IJsonRpcServerCommand
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

            byte[] messageBytes = Convert.FromBase64String(request.Data.Message);

            HashKey16 key;
            bool sent = MessagePool.Send(request.Data.Address, Message_Type.Text, messageBytes, request.Data.Expiration, out key);
            if (sent)
                response.Data.Key = key;
            else
                response.ErrorCode = 200;

            responseObject = response;
            return sent;
        }

        public class Request
        {
            [JsonProperty("address")]
            public string Address { get; set; } = string.Empty;

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;

            [JsonProperty("expiration")]
            public uint Expiration { get; set; } = 3600;
        }

        public class Response
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;
        }
    }
}
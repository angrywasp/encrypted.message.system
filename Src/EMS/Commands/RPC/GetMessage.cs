using AngryWasp.Json.Rpc;
using Newtonsoft.Json;
using System;

namespace EMS.Commands.RPC
{
    [JsonRpcServerCommand("get_message")]
    public class GetMessage : IJsonRpcServerCommand
    {
        public bool Handle(string requestString, out object responseObject)
        {
            JsonRequest<Request> request = null;
            responseObject = null;

            if (!JsonRequest<Request>.Deserialize(requestString, out request))
                return false;

            JsonResponse<Response> response = new JsonResponse<Response>();

            Message message;
            if (!MessagePool.Messages.TryGetValue(request.Data.Key, out message))
            {
                responseObject = response;
                return false;
            }

            response.Data.Key = message.Key;
            response.Data.Hash = message.Hash;
            response.Data.Timestamp = message.Timestamp;
            response.Data.Expiration = message.Expiration;
            response.Data.MessageVersion = message.MessageVersion;
            response.Data.MessageType = message.MessageType;  

            response.Data.Direction = message.Direction.ToString().ToLower();

            if (message.ReadProof != null)
                response.Data.ReadProof = message.ReadProof;

            if (!message.IsDecrypted)
            {
                responseObject = response;
                return true;
            }

            response.Data.Address = message.Address;
            response.Data.Message = Convert.ToBase64String(message.DecryptedData);

            responseObject = response;
            return true;
        }

        public class Request
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;
        }

        public class Response
        {
            [JsonProperty("key")]
            public HashKey16 Key { get; set; } = HashKey16.Empty;

            [JsonProperty("hash")]
            public HashKey32 Hash { get; set; } = HashKey32.Empty;

            [JsonProperty("timestamp")]
            public uint Timestamp { get; set; } = 0;

            [JsonProperty("expiration")]
            public uint Expiration { get; set; } = 0;

            [JsonProperty("version")]
            public byte MessageVersion { get; set; } = 0;

            [JsonProperty("type")]
            public Message_Type MessageType { get; set; } = Message_Type.Invalid;

            [JsonProperty("address")]
            public string Address { get; set; } = string.Empty;

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;

            [JsonProperty("direction")]
            public string Direction { get; set; } = string.Empty;

            [JsonProperty("read_proof")]
            public ReadProof ReadProof { get; set; } = new ReadProof();
        }
    }
}
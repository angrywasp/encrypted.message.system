using Newtonsoft.Json;
using AngryWasp.Helpers;
using System;
using System.Text;

namespace EMS.Commands.RPC
{
    public class GetMessage
    {
        public static bool Handle(object json, out object jsonResult)
        {
            EMS.JsonRequest<JsonRequest> r = (EMS.JsonRequest<JsonRequest>)json;
            EMS.JsonResponse<JsonResponse> ret = new EMS.JsonResponse<JsonResponse>();
            ret.Response = new JsonResponse();

            HashKey16 key = r.Request.Key.FromByteHex();

            IncomingMessage incomingMessage;
            OutgoingMessage outgoingMessage;

            if (MessagePool.IncomingMessages.TryGetValue(key, out incomingMessage))
            {
                ret.Response.IsIncoming = true;
                ret.Response.Timestamp = incomingMessage.TimeStamp;
                ret.Response.Destination = incomingMessage.Sender;
                string messageText = Convert.ToBase64String(Encoding.UTF8.GetBytes(incomingMessage.Message));
                ret.Response.Message = messageText;

                jsonResult = ret;
                return true;
            }

            if (MessagePool.OutgoingMessages.TryGetValue(key, out outgoingMessage))
            {
                ret.Response.IsIncoming = false;
                ret.Response.Timestamp = outgoingMessage.TimeStamp;
                ret.Response.Destination = outgoingMessage.Recipient;
                string messageText = Convert.ToBase64String(Encoding.UTF8.GetBytes(outgoingMessage.Message));
                ret.Response.Message = messageText;

                jsonResult = ret;
                return true;
            }

            jsonResult = ret;
            return false;
        }

        public class JsonRequest
        {
            [JsonProperty("key")]
            public string Key { get; set; }
        }

        public class JsonResponse
        {
            [JsonProperty("incoming")]
            public bool IsIncoming { get; set; }

            [JsonProperty("timestamp")]
            public ulong Timestamp { get; set; }

            [JsonProperty("destination")]
            public string Destination { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}
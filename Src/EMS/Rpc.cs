using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace EMS
{
    //200 = OK
    //400 = Handler error. Error message should be provided as the JSON response
    public enum Response_Code
    {
        OK = 200,
        Error = 400,
    }

    [JsonObject]
    public class JsonRequestBase
    {
        [JsonProperty("api")]
        public uint ApiLevel { get; set; }
    }

    [JsonObject]
    public class JsonResponseBase
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    [JsonObject]
    public class JsonResponse<T> : JsonResponseBase
    {
        [JsonProperty("response")]
        public T Response { get; set; } = default(T);
    }

    [JsonObject]
    public class JsonRequest<T> : JsonRequestBase
    {
        [JsonProperty("request")]
        public T Request { get; set; } = default(T);
    }

    public class RpcServer
    {
        public const uint API_LEVEL = 1;
        HttpListener listener;

        private ushort port = 0;

        public ushort Port => port;

        public delegate bool RpcFunc<T, U>(T args, out U value);

        private static Dictionary<string, Tuple<Type, RpcFunc<object, object>>> commands = new Dictionary<string, Tuple<Type, RpcFunc<object, object>>>();

        public static void RegisterCommand<T>(string key, RpcFunc<object, object> value)
        {
            if (!commands.ContainsKey(key))
                commands.Add(key, new Tuple<Type, RpcFunc<object, object>>(typeof(JsonRequest<T>), value));
        }

        public void Start(ushort port, ushort sslPort)
        {
            this.port = port;
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            listener.Prefixes.Add($"https://*:{sslPort}/");
            //todo: do we need authentication?
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            try
            {
                listener.Start();
            } catch (Exception ex)
            {
                Log.WriteError(ex.Message);
            }
            
            Log.WriteInfo($"Local RPC endpoint on port {port} ({sslPort} SSL)");
            Log.WriteConsole($"Local RPC endpoint on port {port} ({sslPort} SSL)");
            Log.WriteInfo("RPC server initialized");

            Task.Run(() =>
            {
                while(true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HandleRequest(context);
                }
            });
        }

        private void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            response.ContentType = "application/json";

            string text;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                text = reader.ReadToEnd();
            }

            string method = request.Url.Segments[1];
            bool ok = false;
            object resultData = null;

            if (commands.ContainsKey(method))
            {
                try
                {
                    object deserializedRequest = JsonConvert.DeserializeObject(text, commands[method].Item1);

                    if (((JsonRequestBase)deserializedRequest).ApiLevel < API_LEVEL)
                    {
                        resultData = new JsonResponse<string>() {
                            Response = "API level is insufficient" };
                    }
                    else
                        ok = commands[method].Item2.Invoke(deserializedRequest, out resultData);
                }
                catch
                {
                    resultData = new JsonResponse<string>() {
                        Response = "Exception in RPC request"};
                }
            }
            else
            {
                resultData = new JsonResponse<string>() {
                    Response = "The specified method does not exist"};
            }

            response.StatusCode = ok ? (int)Response_Code.OK : (int)Response_Code.Error;
            ((JsonResponseBase)resultData).Status = ok ? "OK" : "ERROR";
            response.OutputStream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(resultData)));
            context.Response.Close();
        }
    }
}
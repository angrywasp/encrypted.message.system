using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nerva.Rpc
{
    public class Log
    {
        public class Presets
        {
            //Log nothing
            public static readonly Log None = new Log();

            //Sane default. Logs errors and only logs rpc traffic when an error occurs
            public static readonly Log Normal = new Log
            {
                LogRpcRequest = true,
                LogRpcResponse = true,
                LogRpcErrors = true,
                LogNetworkErrors = true,
                OnlyLogRpcOnError = true
            };

            //Log everything. Creates a lot of terminal spam
            public static readonly Log Full = new Log
            {
                LogRpcRequest = true,
                LogRpcResponse = true,
                LogRpcErrors = true,
                LogNetworkErrors = true,
                OnlyLogRpcOnError = false
            };
        }

        public List<int> SuppressRpcCodes { get; set; } = new List<int>();
        public bool LogRpcRequest { get; set; } = false;
        public bool LogRpcResponse { get; set; } = false;
        public bool LogRpcErrors { get; set; } = false;
        public bool LogNetworkErrors { get; set; } = false;

        public bool OnlyLogRpcOnError { get; set; } = true;
    }

    public static class Config
    {
        public const uint DEFAULT_PORT = 3500;
        public const string DEFAULT_HOST = "http://127.0.0.1";
    }
}
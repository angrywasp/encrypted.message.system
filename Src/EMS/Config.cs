using System.IO;
using System.Xml.Linq;
using AngryWasp.Serializer;

namespace EMS
{
    public static class Config
    {
        //Set a future time limit to prevent people extending the message life by using a time in the future
        public const uint FTL = 300;

        //Difficulty for the PoW hash of the message is set at (expiration time) * (multiplier)
        public const uint DIFF_MULTIPLIER = 1024;

        //Give messages a minimum life and enforce it to prevent spamming the network with short lived, low diff messages
        public const uint MIN_MESSAGE_EXPIRATION = 3600;

        public const string DEFAULT_KEY_FILE = "default.keys";
        public const string DEFAULT_CONFIG_FILE = "app.config";

        public const string DEFAULT_LOG_FILE = "app.log";

        public const ushort DEFAULT_P2P_PORT = 3500;
        public const ushort DEFAULT_RPC_PORT = 4500;
        public const ushort DEFAULT_RPC_SSL_PORT = 4501;

        private static UserConfig user = null;
        private static string userConfigFile = DEFAULT_CONFIG_FILE;

        public static UserConfig User => user;

        public static void Initialize(string file = DEFAULT_CONFIG_FILE)
        {
            userConfigFile = file;

            if (!File.Exists(file))
                user = new UserConfig();
            else
                user = new ObjectSerializer().Deserialize<UserConfig>(XDocument.Load(file));
        }

        public static void Save() => new ObjectSerializer().Serialize(user, userConfigFile);
    }

    public class UserConfig
    {
        public uint MessageExpiration { get; set; } = Config.MIN_MESSAGE_EXPIRATION;

        public string LogFile { get; set; } = Config.DEFAULT_LOG_FILE;

        public string KeyFile { get; set; } = Config.DEFAULT_KEY_FILE;

        public ushort P2pPort { get; set; } = Config.DEFAULT_P2P_PORT;

        public ushort RpcPort { get; set; } = Config.DEFAULT_RPC_PORT;

        public ushort RpcSslPort { get; set; } = Config.DEFAULT_RPC_SSL_PORT;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using AngryWasp.Helpers;
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

        public const byte MESSAGE_VERSION = 0;

        public const string DEFAULT_KEY_FILE = "default.keys";
        public const string DEFAULT_CONFIG_FILE = "app.config";
        public const string DEFAULT_CACHE_FILE = "app.cache";
        public const string DEFAULT_LOG_FILE = "app.log";

        public const ushort DEFAULT_P2P_PORT = 3500;
        public const ushort DEFAULT_RPC_PORT = 4500;
        public const ushort DEFAULT_RPC_SSL_PORT = 4501;

        private static UserConfig user = null;
        private static List<byte> messageCache = new List<byte>();

        private static string userConfigFile = DEFAULT_CONFIG_FILE;
        private static string messageCacheFile = DEFAULT_CACHE_FILE;

        public static UserConfig User => user;

        public static List<byte> MessageCache => messageCache;

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


    //Attribute to designate which properties can be used as CLI flags
    public class CommandLinePropertyAttribute : Attribute
    {
        private string flag;
        private string description;

        public string Flag => flag;
        public string Description => description;

        public CommandLinePropertyAttribute(string flag, string description)
        {
            this.flag = flag;
            this.description = description;
        }
    }

    //Maps the CLI flags to the config file properties
    public static class ConfigMapper
    {
        public static string[,] IgnoreList = new string[,]
        {
            {"password", "Key file password. Omit to be prompted for a password"},
            {"config-file", "Path to an existing config file to load"}
        };

        public static bool IgnoreListContains(string flag)
        {
            for (int i = 0; i < IgnoreList.GetLength(0); i++)
                if (IgnoreList[i, 0] == flag)
                    return true;
            
            return false;
        }

        private static Dictionary<string, Tuple<CommandLinePropertyAttribute, PropertyInfo>> map = 
            new Dictionary<string, Tuple<CommandLinePropertyAttribute, PropertyInfo>>();

        public static bool Process(CommandLineParser cmd)
        {
            foreach (var p in typeof(UserConfig).GetProperties())
            {
                CommandLinePropertyAttribute a = p.GetCustomAttributes(true).OfType<CommandLinePropertyAttribute>().FirstOrDefault();
                if (a == null)
                    continue;

                map.Add(a.Flag, new Tuple<CommandLinePropertyAttribute, PropertyInfo>(a, p));
            }

            while (cmd.Count > 0)
            {
                CommandLineParserOption opt = cmd.Pop();
                if (opt.Flag == null)
                    continue;

                if (opt.Flag == "help")
                {
                    ShowHelp();
                    return false;
                }

                if (IgnoreListContains(opt.Flag))
                    continue;
                
                //provided a flag wrong
                if (!map.ContainsKey(opt.Flag))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"No flag matches {opt.Flag}");
                    Console.ForegroundColor = ConsoleColor.White;
                    ShowHelp();
                    return false;
                }

                var dat = map[opt.Flag];

                //boolean flags do not need a value
                if (dat.Item2.PropertyType == typeof(bool))
                {
                    dat.Item2.SetValue(Config.User, true);
                    continue;
                }

                //check we have a value if the flag expects a value
                if (string.IsNullOrEmpty(opt.Value))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"No value provided for flag {opt.Flag}");
                    Console.ForegroundColor = ConsoleColor.White;
                    ShowHelp();
                    return false;
                }

                if (!Parse(dat, opt))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Could not parse value for flag {opt.Flag}");
                    Console.ForegroundColor = ConsoleColor.White;
                    ShowHelp();
                    return false;
                }
            }

            return true;
        }

        private static bool Parse(Tuple<CommandLinePropertyAttribute, PropertyInfo> dat, CommandLineParserOption opt)
        {
            if (dat.Item2.PropertyType.IsGenericType)
            {
                try
                {
                    object obj = Serializer.Deserialize(dat.Item2.PropertyType.GenericTypeArguments[0], opt.Value);
                    object instance = dat.Item2.GetValue(Config.User);
                    dat.Item2.PropertyType.GetMethod("Add").Invoke(instance, new object[] { obj });
                    return true;
                }
                catch { return false; }
            }
            else
            {
                try
                {
                    object obj = Serializer.Deserialize(dat.Item2.PropertyType, opt.Value);
                    dat.Item2.SetValue(Config.User, obj);
                    return true;
                }
                catch { return false; }
            }
        }

        private static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("EMS command line help");

            for (int i = 0; i < IgnoreList.GetLength(0); i++)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{("--" + IgnoreList[i, 0]).PadLeft(16)}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($": {IgnoreList[i, 1]}");
            }

            foreach (var i in map.Values)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{("--" + i.Item1.Flag).PadLeft(16)}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($": {i.Item1.Description}");
            }

            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    //Config file which we can use for CLI flags
    public class UserConfig
    {
        [CommandLineProperty("expiration", "Set the expiration time of messages (seconds).")]
        public uint MessageExpiration { get; set; } = Config.MIN_MESSAGE_EXPIRATION;

        [CommandLineProperty("log-file", "Path to the log file to use.")]
        public string LogFile { get; set; } = Config.DEFAULT_LOG_FILE;

        [CommandLineProperty("key-file", "Path to the key file to use.")]
        public string KeyFile { get; set; } = Config.DEFAULT_KEY_FILE;

        [CommandLineProperty("p2p-port", "P2P port. Default 3500")]
        public ushort P2pPort { get; set; } = Config.DEFAULT_P2P_PORT;

        [CommandLineProperty("rpc-port", "RPC port. Default 4500")]
        public ushort RpcPort { get; set; } = Config.DEFAULT_RPC_PORT;

        [CommandLineProperty("rpc-ssl-port", "RPC SSL port. Default 4501")]
        public ushort RpcSslPort { get; set; } = Config.DEFAULT_RPC_SSL_PORT;

        [CommandLineProperty("no-dns-seeds", "Do not fetch seed nodes from DNS")]
        public bool NoDnsSeeds { get; set; } = false;

        [CommandLineProperty("relay-only", "Use this node only for relaying messages. Does not open a key file.")]
        public bool RelayOnly { get; set; } = false;

        [CommandLineProperty("no-user-input", "Restrict node to not accept user input.")]
        public bool NoUserInput { get; set; } = false;

        [CommandLineProperty("cache-incoming", "Save incoming messages to file automatically.")]
        public bool CacheIncoming { get; set; } = false;
        
        [CommandLineProperty("seed-node", "Specify a seed node additional to the DNS nodes.")]
        public List<string> SeedNodes { get; set; } = new List<string>();
    }
}
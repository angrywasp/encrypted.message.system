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

        private static UserConfig user = null;

        public static UserConfig User => user;

        public static void Initialize(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                user = new UserConfig();
            else
                user = new ObjectSerializer().Deserialize<UserConfig>(XDocument.Load(file));
        }
    }

    public class UserConfig
    {
        public uint MessageExpiration { get; set; } = Config.MIN_MESSAGE_EXPIRATION;
    }
}
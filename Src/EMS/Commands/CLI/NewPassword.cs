using System;
using System.IO;
using System.Text;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;

namespace EMS.Commands.CLI
{
    [ApplicationCommand("new_password", "Change the password for the loaded key file")]
    public class NewPassword : IApplicationCommand
    {
        public bool Handle(string command)
        {
            byte[] encryptedKeyData = File.ReadAllBytes(Config.User.KeyFile);
            string a = PasswordPrompt.Get("Enter your key file password");
            byte[] password = string.IsNullOrEmpty(a) ? Keccak.Hash128(HashKey16.Empty) : Keccak.Hash128(Encoding.ASCII.GetBytes(a));
            byte[] decryptedKeyData = null;
                
            try
            {
                decryptedKeyData = Aes.Decrypt(encryptedKeyData, password);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Key file password was incorrect.");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            string b = PasswordPrompt.Get("Enter a password for your key file");
            string c = PasswordPrompt.Get("Confirm your password");

            if (b != c)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Password do not match.");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            password = string.IsNullOrEmpty(b) ? Keccak.Hash128(HashKey16.Empty) : Keccak.Hash128(Encoding.ASCII.GetBytes(b));

            encryptedKeyData = Aes.Encrypt(KeyRing.PrivateKey, password);

            File.WriteAllBytes(Config.User.KeyFile, encryptedKeyData);
            return true;
        }
    }
}
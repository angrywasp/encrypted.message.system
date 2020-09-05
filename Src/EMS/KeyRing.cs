using System.Collections;
using System.IO;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using System.Linq;
using System.Text;
using System;
using AngryWasp.Cli;

namespace EMS
{
    public static class KeyRing
    {
        private static byte[] privateKey;
        private static byte[] publicKey;

        public static byte[] PrivateKey => privateKey;
        public static byte[] PublicKey => publicKey;

        public static void ReadKey(string password = null)
        {
            if (!File.Exists(Config.User.KeyFile))
                NewKey(password);
            else
            {
                byte[] encryptedKeyData = File.ReadAllBytes(Config.User.KeyFile);
                if (password == null)
                    password = PasswordPrompt.Get("Enter your key file password");
                byte[] passwordBytes = string.IsNullOrEmpty(password) ? Keccak.Hash128(HashKey16.Empty) : Keccak.Hash128(Encoding.ASCII.GetBytes(password));
                
                try
                {
                    privateKey = Aes.Decrypt(encryptedKeyData, passwordBytes);
                }
                catch
                {
                    Application.TriggerExit("Key file password was incorrect. Aborting!");
                    return;
                }
                
                publicKey = Ecc.GetPublicKeyFromPrivateKey(privateKey);
                Log.WriteConsole($"Address - {Base58.Encode(publicKey)}");
            }
        }

        public static void NewKey(string password = null)
        {
            Application.PauseBufferedLog(true);
            //Create a new address
            Ecc.GenerateKeyPair(out publicKey, out privateKey);
            Log.WriteConsole($"Address - {Base58.Encode(publicKey)}");

            //Save to the key file path
            if (string.IsNullOrEmpty(Config.User.KeyFile))
                return;

            byte[] passwordBytes = null;

            if (password == null)
            {
                while (true)
                {
                    string a = PasswordPrompt.Get("Enter a password for your key file");
                    string b = PasswordPrompt.Get("Confirm your password");

                    if (a != b)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Password do not match. Please try again");
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    passwordBytes = string.IsNullOrEmpty(a) ? Keccak.Hash128(HashKey16.Empty) : Keccak.Hash128(Encoding.ASCII.GetBytes(a));
                    break;
                }
            }
            else
                passwordBytes = Keccak.Hash128(Encoding.ASCII.GetBytes(password));


            byte[] encryptedKeyData = Aes.Encrypt(privateKey, passwordBytes);

            File.WriteAllBytes(Config.User.KeyFile, encryptedKeyData);
            Application.PauseBufferedLog(false);
        }

        public static void EraseKey()
        {
            privateKey = publicKey = null;
        }

        public static byte[] CreateSharedKey(byte[] recipientPublicKey) => Ecc.CreateKeyAgreement(privateKey, recipientPublicKey);

        public static bool EncryptMessage(byte[] input, string base58RecipientAddress, out byte[] encryptionResult, out byte[] signature, out byte[] addressXor)
        {
            byte[] to;
            encryptionResult = null;
            signature = null;
            addressXor = null;
            if (!Base58.Decode(base58RecipientAddress, out to))
            {
                Log.WriteWarning("Address is invalid");
                return false;
            }

            byte[] sharedKey = CreateSharedKey(to);

            if (sharedKey == null)
            {
                Log.WriteWarning("Address is invalid");
                return false;
            }

            BitArray a = new BitArray(publicKey);
            BitArray b = new BitArray(to);
            addressXor = new byte[publicKey.Length];

            a.Xor(b);
            a.CopyTo(addressXor, 0);

            encryptionResult = Aes.Encrypt(input, sharedKey);
            signature = Ecc.Sign(encryptionResult, privateKey);

            return true;
        }
        

        // The before being sent for decryption a message should have already been validated
        // The validation process extracts some message properties, so we pass in the 
        // validated message to fill the remaining data
        public static Message DecryptMessage(byte[] input, Message validatedMessage)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(input));
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            ushort signatureLength = BitShifter.ToUShort(reader.ReadBytes(2));
            ushort encryptedMessageLength = BitShifter.ToUShort(reader.ReadBytes(2));
            BitArray xorKey = new BitArray(reader.ReadBytes(65));
            HashKey32 readProofHash = reader.ReadBytes(32);
            byte[] xorResult = new byte[65];

            BitArray ba = new BitArray(publicKey);
            ba.Xor(xorKey);
            ba.CopyTo(xorResult, 0);
    
            byte[] signature = reader.ReadBytes(signatureLength);
            byte[] encryptedMessage = reader.ReadBytes(encryptedMessageLength);

            reader.Close();

            // If we can verify this against the xor result, it means it was validated 
            // against a senders address making it an imcoming message
            //
            // If that fails, we try to validate it against our own public key. This would indicate it is an 
            // outgoing message. We may need this in case we sync messages we previously sent from the pool
            // i.e. if we restarted a node. Without this code the verification would fail and they would show as encrypted
            if (Ecc.Verify(encryptedMessage, xorResult, signature))
                validatedMessage.Direction = Message_Direction.In;
            else if (Ecc.Verify(encryptedMessage, publicKey, signature))
                validatedMessage.Direction = Message_Direction.Out;
            else
                return validatedMessage;

            byte[] decrypted = null;

            try
            {
                decrypted = Aes.Decrypt(encryptedMessage, CreateSharedKey(xorResult));
            }
            catch
            {
                Log.WriteError("Message decryption error. Message is corrupt");
            }

            if (decrypted == null)
                return null;

            HashKey16 readProofNonce = decrypted.Take(16).ToArray();
            validatedMessage.MessageType = (Message_Type)decrypted[16];
            validatedMessage.DecryptedData = decrypted.Skip(17).ToArray();
            validatedMessage.Address = Base58.Encode(xorResult);
            validatedMessage.ReadProof = new ReadProof
            {
                Nonce = readProofNonce,
                Hash = readProofHash
            };

            return validatedMessage;
        }
    }
}
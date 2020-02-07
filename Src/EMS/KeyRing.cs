using System.Collections;
using System.IO;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using System.Linq;
using System.Text;

namespace EMS
{
    public static class KeyRing
    {
        private static byte[] privateKey;
        private static byte[] publicKey;

        public static byte[] PrivateKey => privateKey;
        public static byte[] PublicKey => publicKey;

        public static void LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                Ecc.GenerateKeyPair(out publicKey, out privateKey);
            else
            {
                if (File.Exists(path))
                {
                    privateKey = File.ReadAllBytes(path);
                    publicKey = Ecc.GetPublicKeyFromPrivateKey(privateKey);
                }
                else
                {
                    Ecc.GenerateKeyPair(out publicKey, out privateKey);
                    File.WriteAllBytes(path, privateKey);
                }
            }

            Log.WriteConsole($"Address - {Base58.Encode(publicKey)}");
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
        
        public static bool DecryptMessage(byte[] input, out string address, out HashKey16 readProofNonce, out HashKey32 readProofHash, out string message)
        {
            address = null;
            readProofNonce = HashKey16.Empty;
            readProofHash = HashKey32.Empty;
            message = null;

            BinaryReader reader = new BinaryReader(new MemoryStream(input));
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            ushort signatureLength = BitShifter.ToUShort(reader.ReadBytes(2));
            ushort encryptedMessageLength = BitShifter.ToUShort(reader.ReadBytes(2));
            byte[] xorKey = reader.ReadBytes(65);
            readProofHash = reader.ReadBytes(32);
            byte[] a = new byte[65];

            BitArray ba = new BitArray(publicKey);
            BitArray bb = new BitArray(xorKey);
            ba.Xor(bb);
            ba.CopyTo(a, 0);
    
            byte[] signature = reader.ReadBytes(signatureLength);
            byte[] encryptedMessage = reader.ReadBytes(encryptedMessageLength);

            if (!Ecc.Verify(encryptedMessage, a, signature))
                return false;

            byte[] decrypted = Aes.Decrypt(encryptedMessage, CreateSharedKey(a));

            address = Base58.Encode(a);
            readProofNonce = decrypted.Take(16).ToArray();
            message = Encoding.ASCII.GetString(decrypted.Skip(16).ToArray());

            return true;
        }
    }
}
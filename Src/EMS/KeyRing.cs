using System.Collections;
using System.Collections.Generic;
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

        public static bool EncryptMessage(byte[] input, string base58RecipientAddress, out List<byte> result, out byte[] key)
        {
            byte[] to;
            result = null;
            key = null;
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
            key = new byte[publicKey.Length];

            a.Xor(b);
            a.CopyTo(key, 0);

            byte[] encrypted = Aes.Encrypt(input, sharedKey);
            byte[] sig = Ecc.Sign(encrypted, privateKey);
            List<byte> msg = new List<byte>();

            msg.AddRange(BitShifter.ToByte((ushort)sig.Length));
            msg.AddRange(BitShifter.ToByte((ushort)encrypted.Length));

            byte[] readProofKey = input.Skip(8).Take(16).ToArray();

            msg.AddRange(key);
            msg.AddRange(ProvableMessage.GenerateHash(readProofKey));

            msg.AddRange(sig);
            msg.AddRange(encrypted);

            result = msg;
            return true;
        }
        
        public static bool DecryptMessage(byte[] input, out IncomingMessage result)
        {
            result = null;
            BinaryReader reader = new BinaryReader(new MemoryStream(input));
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            ushort sigLen = BitShifter.ToUShort(reader.ReadBytes(2));
            ushort encLen = BitShifter.ToUShort(reader.ReadBytes(2));
            byte[] xorKey = reader.ReadBytes(65);
            HashKey32 proofHash = reader.ReadBytes(32);
            byte[] a = new byte[65];

            BitArray ba = new BitArray(publicKey);
            BitArray bb = new BitArray(xorKey);
            ba.Xor(bb);
            ba.CopyTo(a, 0);
            string address = Base58.Encode(a);

            byte[] sig = reader.ReadBytes(sigLen);
            byte[] enc = reader.ReadBytes(encLen);

            if (!Ecc.Verify(enc, a, sig))
                return false;

            byte[] sharedKey = CreateSharedKey(a);
            byte[] decrypted = Aes.Decrypt(enc, sharedKey);

            ulong ts = BitShifter.ToULong(decrypted);
            HashKey16 proofNonce = decrypted.Skip(8).Take(16).ToArray();
            string msg = Encoding.ASCII.GetString(decrypted.Skip(24).ToArray());

            result = new IncomingMessage(ts, address, msg);
            result.SetReadProof(proofNonce, proofHash);
            return true;
        }
    }
}
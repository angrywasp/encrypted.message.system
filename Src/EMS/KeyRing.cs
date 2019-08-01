using System.Collections;
using System.Collections.Generic;
using System.IO;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using AngryWasp.Logger;

namespace EMS
{
    public static class KeyRing
    {
        private static byte[] privateKey;
        private static byte[] publicKey;

        public static byte[] PrivateKey => privateKey;
        public static byte[] PublicKey => publicKey;

        public static void Initialize()
        {
            Ecc.GenerateKeyPair(out publicKey, out privateKey);
            Log.Instance.Write($"Address - {Base58.Encode(publicKey)}");
        }

        public static byte[] CreateSharedKey(byte[] recipientPublicKey) => Ecc.CreateKeyAgreement(privateKey, recipientPublicKey);

        //we xor the recipients public key with our own
        //then when the message is sent, the other party can xor the data with their own public key
        //to derive the key of the sender and construct the shared secret to verify the signature and
        //decrypt the message

        public static byte[] EncryptMessage(byte[] input, string base58RecipientAddress)
        {
            byte[] to = Base58.Decode(base58RecipientAddress);
            byte[] sharedKey = CreateSharedKey(to);

            BitArray a = new BitArray(publicKey);
            BitArray b = new BitArray(to);
            byte[] key = new byte[publicKey.Length];

            a.Xor(b);
            a.CopyTo(key, 0);

            byte[] encrypted = Aes.Encrypt(input, sharedKey);
            byte[] sig = Ecc.Sign(encrypted, privateKey);
            List<byte> msg = new List<byte>();
            msg.AddRange(BitShifter.ToByte((ushort)sig.Length));
            msg.AddRange(BitShifter.ToByte((ushort)encrypted.Length));
            msg.AddRange(key);
            msg.AddRange(sig);
            msg.AddRange(encrypted);

            return msg.ToArray();
        }
        

        public static byte[] DecryptMessage(byte[] input, int inputOffset, out string address)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(input));
            reader.BaseStream.Seek(inputOffset, SeekOrigin.Begin);

            ushort sigLen = BitShifter.ToUShort(reader.ReadBytes(2));
            ushort encLen = BitShifter.ToUShort(reader.ReadBytes(2));
            byte[] xorKey = reader.ReadBytes(65);
            byte[] a = new byte[65];

            BitArray ba = new BitArray(publicKey);
            BitArray bb = new BitArray(xorKey);
            ba.Xor(bb);
            ba.CopyTo(a, 0);
            address = Base58.Encode(a);

            byte[] sig = reader.ReadBytes(sigLen);
            byte[] enc = reader.ReadBytes(encLen);

            if (!Ecc.Verify(enc, a, sig))
                return null;

            byte[] sharedKey = CreateSharedKey(a);

            return Aes.Decrypt(enc, sharedKey);
        }
    }
}
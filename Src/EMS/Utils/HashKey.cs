using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AngryWasp.Helpers;

namespace EMS
{
    public struct HashKey : IReadOnlyList<byte>, IEquatable<HashKey>, IEquatable<byte[]>
    {
        private readonly byte[] value;

        public byte this[int index]
        {
            get
            {
                if (this.value != null)
                    return this.value[index];

                return default(byte);
            }
        }

        public int Count => 16;

        public static readonly HashKey Empty = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public HashKey(byte[] bytes)
        {
            value = bytes;
        }

        public bool Equals(HashKey other) => this.SequenceEqual(other);

        public bool Equals(byte[] other) => this.SequenceEqual(other);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is HashKey && this.Equals((HashKey)obj);
        }

        public IEnumerator<byte> GetEnumerator()
        {
            if (this.value != null)
                return ((IList<byte>)this.value).GetEnumerator();

            return Enumerable.Repeat(default(byte), 16).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override int GetHashCode()
        {
            if (this.value == null)
                return 0;

            int offset = 0;
            return 
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset);
        }

        public static bool operator ==(HashKey left, HashKey right) => left.Equals(right);

        public static bool operator !=(HashKey left, HashKey right) => !left.Equals(right);

        public static bool operator ==(byte[] left, HashKey right) => right.Equals(left);

        public static bool operator !=(byte[] left, HashKey right) => !right.Equals(left);

        public static bool operator ==(HashKey left, byte[] right) => left.Equals(right);

        public static bool operator !=(HashKey left, byte[] right) => !left.Equals(right);

        public static implicit operator HashKey(byte[] value) => new HashKey(value);

        public static implicit operator byte[](HashKey value) => value;

        public static implicit operator HashKey(List<byte> value) => new HashKey(value.ToArray());

        public override string ToString() => value.ToHex();

        public byte[] ToByte() => value;
    }
}
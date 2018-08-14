using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using System;
using System.IO;
using DbgViewTR;

namespace Neo.Core
{
    /// <summary>
    /// 交易输入
    /// </summary>
    public class CoinReference : IEquatable<CoinReference>, IInteropInterface, ISerializable
    {
        /// <summary>
        /// 引用交易的散列值
        /// </summary>
        public UInt256 PrevHash;
        /// <summary>
        /// 引用交易输出的索引
        /// </summary>
        public ushort PrevIndex;

        public int Size => PrevHash.Size + sizeof(ushort);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            PrevHash = reader.ReadSerializable<UInt256>();
            PrevIndex = reader.ReadUInt16();
            TR.Exit();
        }
        
        /// <summary>
        /// 比较当前对象与指定对象是否相等
        /// </summary>
        /// <param name="other">要比较的对象</param>
        /// <returns>返回对象是否相等</returns>
        public bool Equals(CoinReference other)
        {
            TR.Enter();
            if (ReferenceEquals(this, other)) return TR.Exit(true);
            if (ReferenceEquals(null, other)) return TR.Exit(false);
            return TR.Exit(PrevHash.Equals(other.PrevHash) && PrevIndex.Equals(other.PrevIndex));
        }

        /// <summary>
        /// 比较当前对象与指定对象是否相等
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>返回对象是否相等</returns>
        public override bool Equals(object obj)
        {
            TR.Enter();
            if (ReferenceEquals(this, obj)) return TR.Exit(true);
            if (ReferenceEquals(null, obj)) return TR.Exit(false);
            if (!(obj is CoinReference)) return TR.Exit(false);
            return TR.Exit(Equals((CoinReference)obj));
        }

        /// <summary>
        /// 获得对象的HashCode
        /// </summary>
        /// <returns>返回对象的HashCode</returns>
        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit(PrevHash.GetHashCode() + PrevIndex.GetHashCode());
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(PrevHash);
            writer.Write(PrevIndex);
            TR.Exit();
        }

        /// <summary>
        /// 将交易输入转变为json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public JObject ToJson()
        {
            TR.Enter();
            JObject json = new JObject();
            json["txid"] = PrevHash.ToString();
            json["vout"] = PrevIndex;
            return TR.Exit(json);
        }
    }
}

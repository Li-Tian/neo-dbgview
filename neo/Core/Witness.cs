using Neo.IO;
using Neo.IO.Json;
using System.IO;
using DbgViewTR;

namespace Neo.Core
{
    public class Witness : ISerializable
    {
        public byte[] InvocationScript;
        public byte[] VerificationScript;

        private UInt160 _scriptHash;
        public virtual UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = VerificationScript.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            InvocationScript = reader.ReadVarBytes(65536);
            VerificationScript = reader.ReadVarBytes(65536);
            TR.Exit();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
            TR.Exit();
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public JObject ToJson()
        {
            TR.Enter();
            JObject json = new JObject();
            json["invocation"] = InvocationScript.ToHexString();
            json["verification"] = VerificationScript.ToHexString();
            return TR.Exit(json);
        }
    }
}

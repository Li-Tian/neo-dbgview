using Neo.Cryptography;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.IO;
using System.Linq;
using NoDbgViewTR;

namespace Neo.Core
{
    /// <summary>
    /// 包含一系列签名与验证的扩展方法
    /// </summary>
    public static class Helper
    {
        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            TR.Enter();
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return TR.Exit(ms.ToArray());
            }
        }

        /// <summary>
        /// 根据传入的账户信息，对可签名的对象进行签名
        /// </summary>
        /// <param name="verifiable">要签名的数据</param>
        /// <param name="key">用于签名的账户</param>
        /// <returns>返回签名后的结果</returns>
        public static byte[] Sign(this IVerifiable verifiable, KeyPair key)
        {
            TR.Enter();
            using (key.Decrypt())
            {
                return TR.Exit(Crypto.Default.Sign(verifiable.GetHashData(), key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray()));
            }
        }

        public static UInt160 ToScriptHash(this byte[] script)
        {
            TR.Enter();
            return TR.Exit(new UInt160(Crypto.Default.Hash160(script)));
        }

        internal static bool VerifyScripts(this IVerifiable verifiable)
        {
            TR.Enter();
            UInt160[] hashes;
            try
            {
                hashes = verifiable.GetScriptHashesForVerifying();
            }
            catch (InvalidOperationException)
            {
                return TR.Exit(false);
            }
            if (hashes.Length != verifiable.Scripts.Length) return TR.Exit(false);
            for (int i = 0; i < hashes.Length; i++)
            {
                byte[] verification = verifiable.Scripts[i].VerificationScript;
                if (verification.Length == 0)
                {
                    using (ScriptBuilder sb = new ScriptBuilder())
                    {
                        sb.EmitAppCall(hashes[i].ToArray());
                        verification = sb.ToArray();
                    }
                }
                else
                {
                    if (hashes[i] != verifiable.Scripts[i].ScriptHash) return TR.Exit(false);
                }
                using (StateReader service = new StateReader())
                {
                    ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, verifiable, Blockchain.Default, service, Fixed8.Zero);
                    engine.LoadScript(verification, false);
                    engine.LoadScript(verifiable.Scripts[i].InvocationScript, true);
                    if (!engine.Execute()) return TR.Exit(false);
                    if (engine.EvaluationStack.Count != 1 || !engine.EvaluationStack.Pop().GetBoolean()) return TR.Exit(false);
                }
            }
            return TR.Exit(true);
        }
    }
}

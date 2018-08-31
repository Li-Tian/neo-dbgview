using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using VMArray = Neo.VM.Types.Array;
using VMBoolean = Neo.VM.Types.Boolean;
using NoDbgViewTR;

namespace Neo.VM
{
    public static class Helper
    {
        public static ScriptBuilder Emit(this ScriptBuilder sb, params OpCode[] ops)
        {
            TR.Enter();
            foreach (OpCode op in ops)
                sb.Emit(op);
            return TR.Exit(sb);
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, bool useTailCall = false)
        {
            TR.Enter();
            return TR.Exit(sb.EmitAppCall(scriptHash.ToArray(), useTailCall));
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, params ContractParameter[] parameters)
        {
            TR.Enter();
            for (int i = parameters.Length - 1; i >= 0; i--)
                sb.EmitPush(parameters[i]);
            return TR.Exit(sb.EmitAppCall(scriptHash));
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, string operation)
        {
            TR.Enter();
            sb.EmitPush(false);
            sb.EmitPush(operation);
            sb.EmitAppCall(scriptHash);
            return TR.Exit(sb);
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, string operation, params ContractParameter[] args)
        {
            TR.Enter();
            for (int i = args.Length - 1; i >= 0; i--)
                sb.EmitPush(args[i]);
            sb.EmitPush(args.Length);
            sb.Emit(OpCode.PACK);
            sb.EmitPush(operation);
            sb.EmitAppCall(scriptHash);
            return TR.Exit(sb);
        }

        public static ScriptBuilder EmitAppCall(this ScriptBuilder sb, UInt160 scriptHash, string operation, params object[] args)
        {
            TR.Enter();
            for (int i = args.Length - 1; i >= 0; i--)
                sb.EmitPush(args[i]);
            sb.EmitPush(args.Length);
            sb.Emit(OpCode.PACK);
            sb.EmitPush(operation);
            sb.EmitAppCall(scriptHash);
            return TR.Exit(sb);
        }

        public static ScriptBuilder EmitPush(this ScriptBuilder sb, ISerializable data)
        {
            TR.Enter();
            return TR.Exit(sb.EmitPush(data.ToArray()));
        }

        public static ScriptBuilder EmitPush(this ScriptBuilder sb, ContractParameter parameter)
        {
            TR.Enter();
            switch (parameter.Type)
            {
                case ContractParameterType.Signature:
                case ContractParameterType.ByteArray:
                    sb.EmitPush((byte[])parameter.Value);
                    break;
                case ContractParameterType.Boolean:
                    sb.EmitPush((bool)parameter.Value);
                    break;
                case ContractParameterType.Integer:
                    if (parameter.Value is BigInteger bi)
                        sb.EmitPush(bi);
                    else
                        sb.EmitPush((BigInteger)typeof(BigInteger).GetConstructor(new[] { parameter.Value.GetType() }).Invoke(new[] { parameter.Value }));
                    break;
                case ContractParameterType.Hash160:
                    sb.EmitPush((UInt160)parameter.Value);
                    break;
                case ContractParameterType.Hash256:
                    sb.EmitPush((UInt256)parameter.Value);
                    break;
                case ContractParameterType.PublicKey:
                    sb.EmitPush((ECPoint)parameter.Value);
                    break;
                case ContractParameterType.String:
                    sb.EmitPush((string)parameter.Value);
                    break;
                case ContractParameterType.Array:
                    {
                        IList<ContractParameter> parameters = (IList<ContractParameter>)parameter.Value;
                        for (int i = parameters.Count - 1; i >= 0; i--)
                            sb.EmitPush(parameters[i]);
                        sb.EmitPush(parameters.Count);
                        sb.Emit(OpCode.PACK);
                    }
                    break;
                default:
                        TR.Exit();
                        throw new ArgumentException();
            }
            return TR.Exit(sb);
        }

        public static ScriptBuilder EmitPush(this ScriptBuilder sb, object obj)
        {
            TR.Enter();
            switch (obj)
            {
                case bool data:
                    sb.EmitPush(data);
                    break;
                case byte[] data:
                    sb.EmitPush(data);
                    break;
                case string data:
                    sb.EmitPush(data);
                    break;
                case BigInteger data:
                    sb.EmitPush(data);
                    break;
                case ISerializable data:
                    sb.EmitPush(data);
                    break;
                case sbyte data:
                    sb.EmitPush(data);
                    break;
                case byte data:
                    sb.EmitPush(data);
                    break;
                case short data:
                    sb.EmitPush(data);
                    break;
                case ushort data:
                    sb.EmitPush(data);
                    break;
                case int data:
                    sb.EmitPush(data);
                    break;
                case uint data:
                    sb.EmitPush(data);
                    break;
                case long data:
                    sb.EmitPush(data);
                    break;
                case ulong data:
                    sb.EmitPush(data);
                    break;
                case Enum data:
                    sb.EmitPush(BigInteger.Parse(data.ToString("d")));
                    break;
                default:
                    TR.Exit();
                    throw new ArgumentException();
            }
            return TR.Exit(sb);
        }

        public static ScriptBuilder EmitSysCall(this ScriptBuilder sb, string api, params object[] args)
        {
            TR.Exit();
            for (int i = args.Length - 1; i >= 0; i--)
                EmitPush(sb, args[i]);
            return TR.Exit(sb.EmitSysCall(api));
        }

        public static ContractParameter ToParameter(this StackItem item)
        {
            TR.Enter();
            switch (item)
            {
                case VMArray array:
                    return TR.Exit(new ContractParameter
                    {
                        Type = ContractParameterType.Array,
                        Value = array.Select(p => p.ToParameter()).ToArray()
                    });
                case VMBoolean _:
                    return TR.Exit(new ContractParameter
                    {
                        Type = ContractParameterType.Boolean,
                        Value = item.GetBoolean()
                    });
                case ByteArray _:
                    return TR.Exit(new ContractParameter
                    {
                        Type = ContractParameterType.ByteArray,
                        Value = item.GetByteArray()
                    });
                case Integer _:
                    return TR.Exit(new ContractParameter
                    {
                        Type = ContractParameterType.Integer,
                        Value = item.GetBigInteger()
                    });
                case InteropInterface _:
                    return TR.Exit(new ContractParameter
                    {
                        Type = ContractParameterType.InteropInterface
                    });
                default:
                    TR.Exit();
                    throw new ArgumentException();
            }
        }
    }
}

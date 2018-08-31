using Neo.IO.Json;
using Neo.SmartContract;
using System.Linq;
using NoDbgViewTR;

namespace Neo.Implementations.Wallets.NEP6
{
    internal class NEP6Contract : Contract
    {
        public string[] ParameterNames;
        public bool Deployed;

        public static NEP6Contract FromJson(JObject json)
        {
            TR.Enter();
            if (json == null)
            {
                TR.Exit();
                return null;
            }
            return TR.Exit(new NEP6Contract
            {
                Script = json["script"].AsString().HexToBytes(),
                ParameterList = ((JArray)json["parameters"]).Select(p => p["type"].AsEnum<ContractParameterType>()).ToArray(),
                ParameterNames = ((JArray)json["parameters"]).Select(p => p["name"].AsString()).ToArray(),
                Deployed = json["deployed"].AsBoolean()
            });
        }

        public JObject ToJson()
        {
            TR.Enter();
            JObject contract = new JObject();
            contract["script"] = Script.ToHexString();
            contract["parameters"] = new JArray(ParameterList.Zip(ParameterNames, (type, name) =>
            {
                JObject parameter = new JObject();
                parameter["name"] = name;
                parameter["type"] = type;
                return TR.Exit(parameter);
            }));
            contract["deployed"] = Deployed;
            return TR.Exit(contract);
        }
    }
}

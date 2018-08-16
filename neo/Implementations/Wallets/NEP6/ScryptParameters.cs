using Neo.IO.Json;
using DbgViewTR;

namespace Neo.Implementations.Wallets.NEP6
{
    public class ScryptParameters
    {
        public static ScryptParameters Default { get; } = new ScryptParameters(16384, 8, 8);

        public readonly int N, R, P;

        public ScryptParameters(int n, int r, int p)
        {
            TR.Enter();
            this.N = n;
            this.R = r;
            this.P = p;
            TR.Exit();
        }

        public static ScryptParameters FromJson(JObject json)
        {
            TR.Enter();
            return TR.Exit(new ScryptParameters((int)json["n"].AsNumber(), (int)json["r"].AsNumber(), (int)json["p"].AsNumber()));
        }

        public JObject ToJson()
        {
            TR.Enter();
            JObject json = new JObject();
            json["n"] = N;
            json["r"] = R;
            json["p"] = P;
            return TR.Exit(json);
        }
    }
}

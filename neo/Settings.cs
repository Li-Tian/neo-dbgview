using Microsoft.Extensions.Configuration;
using Neo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using DbgViewTR;

namespace Neo
{
    internal class Settings
    {
        public uint Magic { get; private set; }
        public byte AddressVersion { get; private set; }
        public string[] StandbyValidators { get; private set; }
        public string[] SeedList { get; private set; }
        public IReadOnlyDictionary<TransactionType, Fixed8> SystemFee { get; private set; }
        public uint SecondsPerBlock { get; private set; }

        public static Settings Default { get; private set; }
        static Settings()
        {
            TR.Enter();
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("protocol.json").Build().GetSection("ProtocolConfiguration");
            Default = new Settings(section);
            TR.Exit();
        }

        public Settings(IConfigurationSection section)
        {
            TR.Enter();
            this.Magic = uint.Parse(section.GetSection("Magic").Value);
            this.AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
            this.StandbyValidators = section.GetSection("StandbyValidators").GetChildren().Select(p => p.Value).ToArray();
            this.SeedList = section.GetSection("SeedList").GetChildren().Select(p => p.Value).ToArray();
            this.SystemFee = section.GetSection("SystemFee").GetChildren().ToDictionary(p => (TransactionType)Enum.Parse(typeof(TransactionType), p.Key, true), p => Fixed8.Parse(p.Value));
            this.SecondsPerBlock = GetValueOrDefault(section.GetSection("SecondsPerBlock"), 15u, p => uint.Parse(p));
            TR.Exit();
        }

        public T GetValueOrDefault<T>(IConfigurationSection section, T defaultValue, Func<string, T> selector)
        {
            TR.Enter();
            if (section.Value == null) return TR.Exit(defaultValue);
            return TR.Exit(selector(section.Value));
        }
    }
}

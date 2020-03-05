using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Data.DataModel.Creation {
    public class ReduceableHouseComponent {
        [NPoco.Ignore]
        public double EffectiveEnergyDemand {
            get {
                return LocalnetHighVoltageYearlyTotalElectricityUse + LocalnetLowVoltageYearlyTotalElectricityUse -
                       ReductionEntries.Sum(x => x.Value);
            }
        }

        public double LocalnetHighVoltageYearlyTotalElectricityUse { get; set; }

        public double LocalnetLowVoltageYearlyTotalElectricityUse { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [NotNull]
        [ItemNotNull]
        public List<ReductionEntry> ReductionEntries { get; set; } = new List<ReductionEntry>();

        [NotNull]
        public string ReductionEntriessAsJson {
            get => JsonConvert.SerializeObject(ReductionEntries, Formatting.Indented);
            set => ReductionEntries = JsonConvert.DeserializeObject<List<ReductionEntry>>(value);
        }

        public void SetEnergyReduction([NotNull] string key, double value)
        {
            var re = ReductionEntries.FirstOrDefault(x => x.Name == key);
            if (re == null) {
                re = new ReductionEntry(key, 0);
                ReductionEntries.Add(re);
            }

            re.Value = value;
        }
    }
}
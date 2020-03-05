using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Common;
using Data.DataModel.Export;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SQLite;

namespace Data.DataModel.Creation {
    [NPoco.PrimaryKey(nameof(ID))]
    public class KleinWasserkraft : IHouseComponent {
        public GenerationOrLoad GenerationOrLoad { get; set; } = GenerationOrLoad.Generation;
        [Obsolete("only json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public KleinWasserkraft()
        {
        }

        public KleinWasserkraft([JetBrains.Annotations.NotNull] string name,
                                double lowVoltageYearlyTotalElectricityUse,
                                double highVoltageYearlyTotalElectricityUse,
                                [JetBrains.Annotations.NotNull] List<int> originalISNs,
                                int finalIsn,
                                [CanBeNull] string houseGuid,
                                [JetBrains.Annotations.NotNull] string hausAnschlussGuid,
                                [JetBrains.Annotations.NotNull] string sourceGuid,
                                [JetBrains.Annotations.NotNull] string standort,
                                [JetBrains.Annotations.NotNull] string geschäftspartner,
                                [JetBrains.Annotations.NotNull] string bezeichnung,
                                [JetBrains.Annotations.NotNull] string anlagennummer,
                                [JetBrains.Annotations.NotNull] string status,
                                [JetBrains.Annotations.NotNull] string rlmProfileName)
        {
            Name = name;
            LocalnetLowVoltageYearlyTotalElectricityUse = lowVoltageYearlyTotalElectricityUse;
            LocalnetHighVoltageYearlyTotalElectricityUse = highVoltageYearlyTotalElectricityUse;
            EffectiveEnergyDemand = LocalnetLowVoltageYearlyTotalElectricityUse + LocalnetHighVoltageYearlyTotalElectricityUse;
            OriginalISNs = originalISNs;
            FinalIsn = finalIsn;
            HouseGuid = houseGuid ?? throw new FlaException("No house guid was set");
            HausAnschlussGuid = hausAnschlussGuid;
            Guid = sourceGuid;
            Standort = standort;
            Geschäftspartner = geschäftspartner;
            Bezeichnung = bezeichnung;
            Anlagennummer = anlagennummer;
            Status = status;
            RlmProfileName = rlmProfileName;
        }

        [JetBrains.Annotations.NotNull]
        public string Anlagennummer { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Bezeichnung { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Geschäftspartner { get; set; }

        [PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ISNsAsJson {
            get => JsonConvert.SerializeObject(OriginalISNs, Formatting.Indented);
            set => OriginalISNs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string RlmProfileName { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Status { get; set; }
        public int FinalIsn { get; set; }
        [JetBrains.Annotations.NotNull]
        public string HausAnschlussGuid { get; set; }
        public double LocalnetHighVoltageYearlyTotalElectricityUse { get; set; }

        [NPoco.Ignore]
        [Ignore]
        public HouseComponentType HouseComponentType { get; } = HouseComponentType.Kwkw;

        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        public EnergyType EnergyType { get; set; } = EnergyType.Electricity;

        public double LocalnetLowVoltageYearlyTotalElectricityUse { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Name { get; set; }

        [NPoco.Ignore]
        [Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> OriginalISNs { get; set; } = new List<int>();

        [JetBrains.Annotations.NotNull]
        [NPoco.Ignore]
        [Ignore]
        public string SourceGuid => Guid;

        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Standort { get; set; }

        public double EffectiveEnergyDemand { get; set; }
    }
}
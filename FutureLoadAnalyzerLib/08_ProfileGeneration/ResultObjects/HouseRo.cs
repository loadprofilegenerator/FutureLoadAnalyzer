using System.Collections.Generic;
using Data.Database;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects {
    public class HouseRo {
        public HouseRo([NotNull] string houseName, [CanBeNull] string houseCoordsGwr,
                       [CanBeNull] string houseCoordsLocalnet, [CanBeNull] string hausStandorte, [CanBeNull] string erzeugerIds, [CanBeNull] string houseAdress)
        {
            HouseName = houseName;
            HouseCoordsGwr = houseCoordsGwr;
            HouseCoordsLocalnet = houseCoordsLocalnet;
            HausStandorte = hausStandorte;
            ErzeugerIds = erzeugerIds;
            HouseAdress = houseAdress;
        }

        [NotNull]
        [ItemNotNull]
        [RowBuilderIgnore]
        public List<HausAnschlussRo> HausAnschlussList { get; } = new List<HausAnschlussRo>();

        [CanBeNull]
        public string HausStandorte { get; }

        [CanBeNull]
        public string ErzeugerIds { get; }

        [CanBeNull]
        public string HouseAdress { get; }

        [CanBeNull]
        public string HouseCoordsGwr { get; }

        [CanBeNull]
        public string HouseCoordsLocalnet { get; }

        [NotNull]
        public string HouseName { get; }

        public int NumberOfComponents { get; set; }
        public double TimeUsed { get; set; }
        [CanBeNull]
        public string LpgCalculationStatus { get; set; }

        [NotNull]
        public RowBuilder ToRowBuilder()
        {
            return RowBuilder.GetAllProperties(this);
            /*
        RowBuilder.Start("HouseName", HouseName).Add("Time use for Processing [ms]", TimeUsed)
        .Add("Number of Components", NumberOfComponents).Add("GWR Koordinaten", HouseCoordsGwr)
                    .Add("Localnet Koordinaten", HouseCoordsLocalnet)
                    .Add("Haus Standorte", HausStandorte).Add("ErzeugerIDs", ErzeugerIds).Add("LpgCa");*/
        }
    }
}
using System.Collections.Generic;
using Data.Database;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.ResultObjects {
    public class HausAnschlussRo {
        public HausAnschlussRo([CanBeNull] string hausAnschlussName,
                               [NotNull] string objektID,
                               [NotNull] string trafokreis,
                               [NotNull] string hausanschlussGuid,
                               [NotNull] string isn,
                               double lon,
                               double lat,
                               [NotNull] string haStatus,
                               [CanBeNull] string haStandort)
        {
            HausAnschlussName = hausAnschlussName;
            ObjektID = objektID;
            Trafokreis = trafokreis;
            HausanschlussGuid = hausanschlussGuid;
            Isns = isn;
            Lon = lon;
            Lat = lat;
            HaStatus = haStatus;
            HaStandort = haStandort;
        }

        public double AssignmentDistance { get; set; }

        [CanBeNull]
        public string AssignmentMethod { get; set; }

        [CanBeNull]
        public string HaStandort { get; }

        [NotNull]
        public string HaStatus { get; }

        public double HausanschlussEnergyDuringDay { get; set; }

        public double HausanschlussEnergyDuringNight { get; set; }

        [NotNull]
        public string HausanschlussGuid { get; }

        [CanBeNull]
        public string HausAnschlussName { get; }

        [NotNull]
        [ItemNotNull]
        public List<HouseComponentRo> HouseComponents { get; } = new List<HouseComponentRo>();

        [NotNull]
        public string Isns { get; }

        public double Lat { get; }

        public double Lon { get; }
        public double MaximumPower { get; set; }

        [NotNull]
        public string ObjektID { get; }

        [NotNull]
        public string Trafokreis { get; }

        [NotNull]
        public RowBuilder ToRowBuilder([CanBeNull] HouseRo house, XlsResultOutputMode mode)
        {
            var rb = RowBuilder.Start("Hausanschluss Name", HausAnschlussName);
            rb.Add("ObjektID", ObjektID);
            rb.Add("Trafokreis", Trafokreis);
            rb.Add("Hausanschluss Energy During the Night", HausanschlussEnergyDuringNight);
            rb.Add("Hausanschluss Energy During the Day", HausanschlussEnergyDuringDay);
            rb.Add("ISNs", Isns);
            rb.Add("House Assignment Method", AssignmentMethod);
            rb.Add("Assignment Distance", AssignmentDistance);
            rb.Add("Longitude", Lon);
            rb.Add("Latitude", Lat);
            rb.Add("Hausanschluss Status", HaStatus);
            rb.Add("Maximum Anschluss Power", MaximumPower);
            rb.Add("HAStandort", HaStandort);

            if (mode == XlsResultOutputMode.FullLine && house != null) {
                rb.Merge(house.ToRowBuilder());
            }

            return rb;
        }
    }
}
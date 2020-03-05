using System;
using System.Diagnostics.CodeAnalysis;
using Common.Database;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data.DataModel.Creation {
    public class Hausanschluss : IGuidProvider {
        // ReSharper disable once UnusedMember.Global
        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public Hausanschluss()
        {
        }

        public Hausanschluss([NotNull] string hausanschlussGuid,
                             [NotNull] string houseGuid,
                             [NotNull] string objectID,
                             long egid,
                             int isn,
                             double lon,
                             double lat,
                             [NotNull] string trafokreis,
                             HouseMatchingType matchingType,
                             double distance,
                             [CanBeNull] string adress,
                             [CanBeNull] string standort)
        {
            Adress = adress;
            Standort = standort;
            Guid = hausanschlussGuid;
            HouseGuid = houseGuid;
            ObjectID = objectID;
            Egid = egid;
            Isn = isn;
            Lon = lon;
            Lat = lat;
            Trafokreis = trafokreis;
            MatchingType = matchingType;
            Distance = distance;
        }

        [CanBeNull]
        public string Adress { get; set; }

        public double Distance { get; set; }
        public long Egid { get; set; }

        [NotNull]
        public string HouseGuid { get; set; }

        public int Isn { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public HouseMatchingType MatchingType { get; set; }

        [NotNull]
        public string ObjectID { get; set; }

        [CanBeNull]
        public string Standort { get; set; }

        [NotNull]
        public string Trafokreis { get; set; }

        [NotNull]
        public string Guid { get; set; }

        public int ID { get; set; }
    }
}
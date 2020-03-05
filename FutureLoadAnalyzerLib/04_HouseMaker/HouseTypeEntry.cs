using System;
using System.Diagnostics.CodeAnalysis;
using Common.Database;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class HouseTypeEntry : IGuidProvider {
        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public HouseTypeEntry()
        {
        }

        public HouseTypeEntry([NotNull] string houseGuid, HouseType houseType, [NotNull] string valueDictionary,
                              [NotNull] string housetypeentryguid)
        {
            HouseGuid = houseGuid;
            HouseType = houseType;
            ValueDictionary = valueDictionary;
            Guid = housetypeentryguid;
        }

        public string Guid { get; set; }
        public int ID { get; set; }
        [NotNull]
        public string HouseGuid { get; set; }
        public HouseType HouseType { get; set; }
        [NotNull]
        public string ValueDictionary { get; set; }
    }
}
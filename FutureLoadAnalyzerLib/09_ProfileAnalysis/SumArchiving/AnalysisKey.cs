using System;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.Export;
using JetBrains.Annotations;
using MessagePack;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis.SumArchiving {
    [MessagePackObject]
    public struct AnalysisKey : IEquatable<AnalysisKey> {
        public bool Equals(AnalysisKey other) => Trafokreis == other.Trafokreis && ProviderType == other.ProviderType && SumType == other.SumType &&
                                                 GenerationOrLoad == other.GenerationOrLoad && HouseName == other.HouseName &&
                                                 ProfileSource == other.ProfileSource && HouseComponentType == other.HouseComponentType;

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked {
                var hashCode = (Trafokreis != null ? Trafokreis.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProviderType != null ? ProviderType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)SumType;
                hashCode = (hashCode * 397) ^ (int)GenerationOrLoad;
                hashCode = (hashCode * 397) ^ (HouseName != null ? HouseName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProfileSource!= null ? ProfileSource.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (HouseComponentType != null ? HouseComponentType.GetHashCode() : 0);
                return hashCode;
            }
        }

        public AnalysisKey([CanBeNull] string trafokreis,
                           [CanBeNull] string providerType,
                           SumType sumType,
                           GenerationOrLoad generationOrLoad,
                           [CanBeNull] string houseName,
                           [CanBeNull] string profileSource,
                           [CanBeNull] string houseComponentType)
        {
            Trafokreis = trafokreis;
            ProviderType = providerType;
            SumType = sumType;
            GenerationOrLoad = generationOrLoad;
            HouseName = houseName;
            ProfileSource = profileSource;
            HouseComponentType = houseComponentType;
        }

        [Key(0)]
        [CanBeNull]
        public string Trafokreis { get; set; }

        [Key(1)]
        [CanBeNull]
        public string ProviderType { get; set; }

        [Key(2)]
        public SumType SumType { get; set; }

        [Key(3)]
        public GenerationOrLoad GenerationOrLoad { get; set; }

        [Key(4)]
        [CanBeNull]
        public string HouseName { get; set; }

        [CanBeNull]
        [Key(5)]
        public string ProfileSource { get; set; }
        [Key(6)]
        [CanBeNull]
        public string HouseComponentType { get; set; }


        public override bool Equals(object obj) => obj is AnalysisKey other && Equals(other);


        public override string ToString() => GenerationOrLoad + " # " + SumType + " # " + Trafokreis + " # " + ProviderType + " # " + HouseName +
                                             " # " + ProfileSource + " # " + HouseComponentType;

        public static bool operator ==(AnalysisKey left, AnalysisKey right) => left.Equals(right);

        public static bool operator !=(AnalysisKey left, AnalysisKey right) => !left.Equals(right);
    }

    // ReSharper disable once InconsistentNaming
}
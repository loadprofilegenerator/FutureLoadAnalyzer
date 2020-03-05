using System.Collections.Generic;
using Data.DataModel.Creation;
using Data.DataModel.ProfileImport;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class DBDto {
        public DBDto([NotNull] [ItemNotNull] List<House> houses,
                     [NotNull] [ItemNotNull] List<Hausanschluss> hausanschlusse,
                     [NotNull] [ItemNotNull] List<Car> cars,
                     [NotNull] [ItemNotNull] List<Household> households,
                     [NotNull] [ItemNotNull] List<RlmProfile> measuredRlmProfiles)
        {
            Houses = houses;
            Hausanschlusse = hausanschlusse;
            Cars = cars;
            Households = households;
            MeasuredRlmProfiles = measuredRlmProfiles;
        }

        [NotNull]
        [ItemNotNull]
        public List<Car> Cars { get; }

        [NotNull]
        [ItemNotNull]
        public List<Hausanschluss> Hausanschlusse { get; }

        [NotNull]
        [ItemNotNull]
        public List<Household> Households { get; }

        [NotNull]
        [ItemNotNull]
        public List<House> Houses { get; }

        [NotNull]
        [ItemNotNull]
        public List<RlmProfile> MeasuredRlmProfiles { get; }
    }
}
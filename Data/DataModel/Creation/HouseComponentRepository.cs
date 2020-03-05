using System.Collections.Generic;
using Common.Database;
using Common.Steps;
using JetBrains.Annotations;

namespace Data.DataModel.Creation {
    public class HouseComponentRepository {
        public HouseComponentRepository([NotNull] MyDb mydb)
        {
            Households = mydb.FetchAsRepo<Household>();
            ProcessComponents(Households);
            Businesses = mydb.FetchAsRepo<BusinessEntry>();
            ProcessComponents(Businesses);
            PVSystems = mydb.FetchAsRepo<PvSystemEntry>();
            ProcessComponents(PVSystems);
            CarDistanceEntries = mydb.FetchAsRepo<CarDistanceEntry>();
            ProcessComponents(CarDistanceEntries);
            BuildingInfrastructures = mydb.FetchAsRepo<BuildingInfrastructure>();
            ProcessComponents(BuildingInfrastructures);
            Wasserkraft = mydb.FetchAsRepo<KleinWasserkraft>();
            ProcessComponents(Wasserkraft);
            HeatingSystemEntries = mydb.FetchAsRepo<HeatingSystemEntry>();
            ProcessComponents(HeatingSystemEntries);
            AirConditioningEntries = mydb.FetchAsRepo<AirConditioningEntry>();
            ProcessComponents(AirConditioningEntries);
            DhwEntries = mydb.FetchAsRepo<DHWHeaterEntry>();
            ProcessComponents(DhwEntries);
        }

        public HouseComponentRepository([NotNull] AnalysisRepository repo,
                                        [NotNull] ScenarioSliceParameters slice)
        {
            Households = repo.GetSlice(slice).Fetch<Household>();
            ProcessComponents(Households);
            Businesses = repo.GetSlice(slice).Fetch<BusinessEntry>();
            ProcessComponents(Businesses);
            PVSystems = repo.GetSlice(slice).Fetch<PvSystemEntry>();
            ProcessComponents(PVSystems);
            CarDistanceEntries = repo.GetSlice(slice).Fetch<CarDistanceEntry>();
            ProcessComponents(CarDistanceEntries);
            BuildingInfrastructures = repo.GetSlice(slice).Fetch<BuildingInfrastructure>();
            ProcessComponents(BuildingInfrastructures);
            Wasserkraft = repo.GetSlice(slice).Fetch<KleinWasserkraft>();
            ProcessComponents(Wasserkraft);
            HeatingSystemEntries = repo.GetSlice(slice).Fetch<HeatingSystemEntry>();
            ProcessComponents(HeatingSystemEntries);
            AirConditioningEntries = repo.GetSlice(slice).Fetch<AirConditioningEntry>();
            ProcessComponents(AirConditioningEntries);
            DhwEntries = repo.GetSlice(slice).Fetch<DHWHeaterEntry>();
            ProcessComponents(DhwEntries);
        }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<AirConditioningEntry> AirConditioningEntries { get; }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<BuildingInfrastructure> BuildingInfrastructures { get; }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<BusinessEntry> Businesses { get; }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<CarDistanceEntry> CarDistanceEntries { get; }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<DHWHeaterEntry> DhwEntries { get; }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<HeatingSystemEntry> HeatingSystemEntries { get; }

        [NotNull]
        public Dictionary<string, List<IHouseComponent>> HouseComponentsByHouseGuid { get; } = new Dictionary<string, List<IHouseComponent>>();

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<Household> Households { get; }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<PvSystemEntry> PVSystems { get; }

        [NotNull]
        [ItemNotNull]
        public SingleTypeCollection<KleinWasserkraft> Wasserkraft { get; }

        private void ProcessComponents<T>([NotNull] [ItemNotNull]
                                          SingleTypeCollection<T> components) where T : class, IHouseComponent
        {
            foreach (var component in components) {
                var houseguid = component.HouseGuid;
                if (!HouseComponentsByHouseGuid.ContainsKey(houseguid)) {
                    HouseComponentsByHouseGuid.Add(houseguid, new List<IHouseComponent>());
                }

                HouseComponentsByHouseGuid[houseguid].Add(component);
            }
        }
    }
}
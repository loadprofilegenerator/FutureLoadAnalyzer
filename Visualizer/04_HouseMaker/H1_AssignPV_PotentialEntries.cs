using System;
using System.Collections.Generic;
using BurgdorfStatistics._00_Import;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;

namespace BurgdorfStatistics._04_HouseMaker {
// ReSharper disable once InconsistentNaming
    internal class H1_AssignPV_PotentialEntries : RunableWithBenchmark {
        public H1_AssignPV_PotentialEntries([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(H1_AssignPV_PotentialEntries), Stage.Houses,
                900, services, false, new PVPotentialCharts())
        {
            DevelopmentStatus.Add("not implemented: make potential pv system entries");
            DevelopmentStatus.Add("not implemented: add non-assigned entries based on distance to the nearest house to catch the missing 25 gwh");
            DevelopmentStatus.Add("convert all entries to the same coordinate system");
            DevelopmentStatus.Add("compare sonnendach heizungsschätzung zu ebbe und realen gasdaten");
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<PVPotential>(Stage.Houses, Constants.PresentSlice);
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouses.Fetch<House>();
            var sonnendach = dbRaw.Fetch<B05_SonnendachGeoJson>();
            var sonnendachByEgid = new Dictionary<long, List<B05_SonnendachGeoJson>>();
            //var unassignedGeoJsons = new List<SonnendachGeoJson>();
            foreach (var geoJson in sonnendach) {
                if (!geoJson.Feature.Properties.ContainsKey("GWR_EGID")) {
                    continue;
                }

                if (geoJson.Feature.Properties["GWR_EGID"] == null) {
                    //      unassignedGeoJsons.Add(geoJson);
                    continue;
                }

                var egid = (long)geoJson.Feature.Properties["GWR_EGID"];
                if (!sonnendachByEgid.ContainsKey(egid)) {
                    sonnendachByEgid.Add(egid, new List<B05_SonnendachGeoJson>());
                }

                sonnendachByEgid[egid].Add(geoJson);
            }

            dbHouses.BeginTransaction();
            foreach (var house in houses) {
                if (house.EGIDs.Count == 0) {
                    continue;
                }

                foreach (long eGid in house.EGIDs) {
                    if (!sonnendachByEgid.ContainsKey(eGid)) {
                        continue;
                    }

                    var entries = sonnendachByEgid[eGid];
                    foreach (var geoJson in entries) {
                        var pvp = new PVPotential(house.HouseGuid) {
                            Ausrichtung = GetDouble(geoJson.Feature.Properties, "AUSRICHTUNG"),
                            Neigung = GetDouble(geoJson.Feature.Properties, "NEIGUNG"),
                            GesamtStrahlung = GetDouble(geoJson.Feature.Properties, "GSTRAHLUNG"),
                            MittlereStrahlung = GetDouble(geoJson.Feature.Properties, "MSTRAHLUNG"),
                            SonnendachStromErtrag = GetDouble(geoJson.Feature.Properties, "STROMERTRAG"),
                            SonnendachBedarfHeizung = GetDouble(geoJson.Feature.Properties, "BEDARF_HEIZUNG"),
                            SonnendachBedarfWarmwasser = GetDouble(geoJson.Feature.Properties, "BEDARF_WARMWASSER"),
                            PotentialGuid =  Guid.NewGuid().ToString()
                        };
                        dbHouses.Save(pvp);
                    }
                }
            }

            dbHouses.CompleteTransaction();
        }

        private double GetDouble([JetBrains.Annotations.NotNull] IDictionary<string, object> featureProperties, [JetBrains.Annotations.NotNull] string key)
        {
            var o = featureProperties[key];
            if (o is long) {
                return (long)o;
            }

            if (o is double) {
                return (double)o;
            }

            throw new Exception("Unknown data type");
        }
    }
}
using System;
using System.Collections.Generic;
using Common;
using Common.Database;
using Common.Steps;
using Data.Database;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using Visualizer.Visualisation.SingleSlice;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
// ReSharper disable once InconsistentNaming
    public class H1_AssignPV_PotentialEntries : RunableWithBenchmark {
        public H1_AssignPV_PotentialEntries([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(H1_AssignPV_PotentialEntries), Stage.Houses,
                900, services, false, new PVPotentialCharts(services,Stage.Houses))
        {
            DevelopmentStatus.Add("not implemented: make potential pv system entries");
            DevelopmentStatus.Add("not implemented: add non-assigned entries based on distance to the nearest house to catch the missing 25 gwh");
            DevelopmentStatus.Add("convert all entries to the same coordinate system");
            DevelopmentStatus.Add("compare sonnendach heizungsschätzung zu ebbe und realen gasdaten");
        }

        protected override void RunChartMaking()
        {
            AnalysisRepository rp = new AnalysisRepository(Services.RunningConfig);
            var slice = rp.GetSlice(Constants.PresentSlice);
            var potentials = slice.Fetch<PVPotential>();
            RowCollection rc = new RowCollection("pv","pv");
            foreach (var potential in potentials) {
                rc.Add(RowBuilder.Start("Neigung",potential.Neigung)
                    .Add("Ausrichtung",potential.Ausrichtung)
                    .Add("Fläche",potential.SonnendachStromErtrag));
            }
            var fn = MakeAndRegisterFullFilename("PVpotentials.xlsx", Constants.PresentSlice);
            XlsxDumper.WriteToXlsx(fn,rc);

        }

        protected override void RunActualProcess()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouses.RecreateTable<PVPotential>();
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
                        var pvp = new PVPotential(house.Guid,Guid.NewGuid().ToString()) {
                            Ausrichtung = GetDouble(geoJson.Feature.Properties, "AUSRICHTUNG"),
                            Neigung = GetDouble(geoJson.Feature.Properties, "NEIGUNG"),
                            GesamtStrahlung = GetDouble(geoJson.Feature.Properties, "GSTRAHLUNG"),
                            MittlereStrahlung = GetDouble(geoJson.Feature.Properties, "MSTRAHLUNG"),
                            SonnendachStromErtrag = GetDouble(geoJson.Feature.Properties, "STROMERTRAG"),
                            SonnendachBedarfHeizung = GetDouble(geoJson.Feature.Properties, "BEDARF_HEIZUNG"),
                            SonnendachBedarfWarmwasser = GetDouble(geoJson.Feature.Properties, "BEDARF_WARMWASSER"),
                        };
                        dbHouses.Save(pvp);
                    }
                }
            }

            dbHouses.CompleteTransaction();
        }

        private static double GetDouble([JetBrains.Annotations.NotNull] IDictionary<string, object> featureProperties, [JetBrains.Annotations.NotNull] string key)
        {
            var o = featureProperties[key];
            if (o is long l) {
                return l;
            }

            if (o is double d) {
                return d;
            }

            throw new Exception("Unknown data type");
        }
    }
}
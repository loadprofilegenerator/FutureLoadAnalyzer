using System;
using System.Linq;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class H2_DistributePVSystems : RunableWithBenchmark {
        public H2_DistributePVSystems([NotNull] ServiceRepository services)
            : base(nameof(H2_DistributePVSystems), Stage.Houses, 910, services,
                false, new PVInstalledCharts())
        {
            DevelopmentStatus.Add("make chart comparing existing pv to potential pv");
            DevelopmentStatus.Add("//todo: chart for houses with households without PV");
            DevelopmentStatus.Add("//todo: chart for houses that have real pv and no sonnendach");
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<PvSystemEntry>(Stage.Houses, Constants.PresentSlice);
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouses.Fetch<House>();
            var pvanlagen = dbRaw.Fetch<LocalnetPVAnlage>();
            var pvPotentials = dbHouses.Fetch<PVPotential>();
            dbHouses.BeginTransaction();
            double totalPowerOfIgnoredSystems = 0;
            foreach (var house in houses) {
                if (house.ErzeugerIDs.Count == 0) {
                    continue;
                }

                foreach (var houseErzeugerID in house.ErzeugerIDs) {
                    if (houseErzeugerID.StartsWith("PV")) {
                        var pvl = pvanlagen.FirstOrDefault(x => x.Anlagenummer == houseErzeugerID);
                        if (pvl == null) {
                            continue;
                        }

                        var hausanschlussguid = house.Hausanschluss[0].HausanschlussGuid;
                        var pse = new PvSystemEntry(house.HouseGuid, Guid.NewGuid().ToString(),hausanschlussguid, house.ComplexName);
                        var areas = pvPotentials.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                        foreach (var area in areas) {
                            pse.PVAreas.Add(new PVSystemArea(area.Ausrichtung,area.Neigung,area.SonnendachStromErtrag));
                        }
                        int dstIdx = Services.Rnd.Next(house.Hausanschluss.Count);
                        pse.HausAnschlussGuid = house.Hausanschluss[dstIdx].HausanschlussGuid;
                        if (pse.PVAreas.Count == 0)
                        {
                            pse.PVAreas.Add(new PVSystemArea(0,30,pvl.Leistungkwp*1000));
                            Info("No PV System areas defined: " + house.ComplexName+ " Power: " + pvl.Leistungkwp);
                            totalPowerOfIgnoredSystems += pvl.Leistungkwp;
                        }
                        dbHouses.Save(pse);
                    }
                }
            }

            double totalpower = pvanlagen.Sum(x => x.Leistungkwp);
            Info("Total Ignored Power because no Sonnendach data is available:" + totalPowerOfIgnoredSystems + " / " + totalpower);
            dbHouses.CompleteTransaction();
        }
    }
}
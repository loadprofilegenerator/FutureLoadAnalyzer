using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class H2_DistributePVSystems : RunableWithBenchmark {
        public H2_DistributePVSystems([NotNull] ServiceRepository services) : base(nameof(H2_DistributePVSystems),
            Stage.Houses,
            910,
            services,
            false,
            new PVInstalledCharts(services, Stage.Houses))
        {
            DevelopmentStatus.Add("make chart comparing existing pv to potential pv");
            DevelopmentStatus.Add("//todo: chart for houses with households without PV");
            DevelopmentStatus.Add("//todo: chart for houses that have real pv and no sonnendach");
        }

        protected override void RunActualProcess()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            dbHouses.RecreateTable<PvSystemEntry>();
            var houses = dbHouses.Fetch<House>();
            var localnetPVAnlagen = dbRaw.Fetch<LocalnetPVAnlage>();
            var pvPotentials = dbHouses.Fetch<PVPotential>();
            var hausanschlusses = dbHouses.Fetch<Hausanschluss>();
            dbHouses.BeginTransaction();
            double totalEnergyOfFakeSystems = 0;
            double totalEnergyOfSonnendachSystems = 0;
            foreach (var house in houses) {
                if (house.ErzeugerIDs.Count == 0) {
                    continue;
                }

                foreach (var houseErzeugerID in house.ErzeugerIDs) {
                    if (houseErzeugerID.StartsWith("PV")) {
                        var pvl = localnetPVAnlagen.FirstOrDefault(x => x.Anlagenummer == houseErzeugerID);
                        if (pvl == null) {
                            continue;
                        }

                        var hausanschluss = house.GetHausanschlussByIsn(new List<int>(), null, hausanschlusses, MyLogger) ??
                                            throw new FlaException("no hausanschluss");
                        if (hausanschluss.ObjectID.ToLower().Contains("leuchte")) {
                            throw new FlaException("PV anlage an einer leuchte!");
                        }

                        var pse = new PvSystemEntry(house.Guid,
                            Guid.NewGuid().ToString(),
                            hausanschluss.Guid,
                            house.ComplexName,
                            houseErzeugerID,
                            Constants.PresentSlice.DstYear);
                        var areas = pvPotentials.Where(x => x.HouseGuid == house.Guid).ToList();
                        foreach (var area in areas) {
                            pse.PVAreas.Add(new PVSystemArea(area.Ausrichtung, area.Neigung, area.SonnendachStromErtrag));
                        }

                        double localnetTargetEnergy = pvl.Leistungkwp * 1100;
                        if (pse.PVAreas.Count == 0) {
                            pse.PVAreas.Add(new PVSystemArea(0, 30, localnetTargetEnergy));
                            Debug("No PV System areas defined: " + house.ComplexName + ", setting 30°/south system with power: " + pvl.Leistungkwp);
                            totalEnergyOfFakeSystems += pse.PVAreas.Sum(x => x.Energy);
                        }
                        else {
                            double sonnendachEnergy = pse.PVAreas.Sum(x => x.Energy);
                            if (sonnendachEnergy > localnetTargetEnergy) {
                                //need to
                                var potentialAreas = pse.PVAreas.ToList();
                                if (potentialAreas.Count == 0) {
                                    throw new FlaException("No area?");
                                }

                                potentialAreas.Sort((x, y) => y.Energy.CompareTo(x.Energy));
                                pse.PVAreas.Clear();
                                double sumSoFar = 0;
                                while (potentialAreas[0].Energy + sumSoFar < localnetTargetEnergy) {
                                    pse.PVAreas.Add(potentialAreas[0]);
                                    sumSoFar += potentialAreas[0].Energy;
                                    potentialAreas.RemoveAt(0);
                                }

                                double missingEnergy = localnetTargetEnergy - pse.PVAreas.Sum(x => x.Energy);
                                pse.PVAreas.Add(new PVSystemArea(0, 30, missingEnergy));
                                totalEnergyOfSonnendachSystems += pse.PVAreas.Sum(x => x.Energy);
                            }
                            else {
                                //ignore the sonnendach stuff, since it seems to be wrong
                                pse.PVAreas.Clear();
                                pse.PVAreas.Add(new PVSystemArea(0, 30, localnetTargetEnergy));
                                totalEnergyOfFakeSystems += pse.PVAreas.Sum(x => x.Energy);
                            }
                        }

                        dbHouses.Save(pse);
                    }
                }
            }

            double totalpower = localnetPVAnlagen.Sum(x => x.Leistungkwp) * 1100;
            Info("Total arbitrary south energy because no Sonnendach data is available:" + totalEnergyOfFakeSystems / 1000 + "mwh");
            Info("Total  Sonnendach data is available:" + totalEnergyOfSonnendachSystems / 1000 + "mwh");
            Info("Total energy target: " + totalpower / 1000);
            dbHouses.CompleteTransaction();
        }
    }
}
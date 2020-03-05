using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using FutureLoadAnalyzerLib.Visualisation.SingleSlice;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class F_HeatingSystemChanger : RunableForSingleSliceWithBenchmark {
        public F_HeatingSystemChanger([NotNull] ServiceRepository services) : base(nameof(F_HeatingSystemChanger),
            Stage.ScenarioCreation,
            600,
            services,
            true,
            new HeatingSystemCharts(services, Stage.ScenarioCreation))
        {
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<HeatingSystemEntry>();
            var srcHeatingSystems = dbSrcHouses.Fetch<HeatingSystemEntry>();
            var srcHouses = dbSrcHouses.Fetch<House>();
            var dstHouses = dbDstHouses.FetchAsRepo<House>();
            if (srcHeatingSystems.Count == 0) {
                throw new FlaException("No heating systems were found");
            }

            double totalHeatingDemand = srcHeatingSystems.Sum(x => x.HeatDemand);
            double totalEbf = srcHouses.Sum(x => x.EnergieBezugsFläche);
            double averageEnergyIntensityForNewAppartments = totalHeatingDemand / totalEbf * 0.5;

            //add heating demand from new appartments
            foreach (var hse in srcHeatingSystems) {
                var house = dstHouses.GetByGuid(hse.HouseGuid);
                foreach (var expansion in house.Appartments) {
                    var hseExpansion = hse.HeatDemands.FirstOrDefault(x => x.HouseExpansionGuid == expansion.Guid);
                    if (hseExpansion == null) {
                        AppartmentHeatingDemand hsex = new AppartmentHeatingDemand(expansion.Guid,
                            expansion.EnergieBezugsFläche,
                            averageEnergyIntensityForNewAppartments * expansion.EnergieBezugsFläche,
                            slice.DstYear);
                        hse.HeatDemands.Add(hsex);
                    }
                }
            }

            var dstHousesByGuid = dstHouses.ToDictionary(x => x.Guid, x => x);

            DumpHeatingSystemsToXlsx(slice, srcHeatingSystems, dstHousesByGuid);

            //building renovations
            WeightedRandomAllocator<HeatingSystemEntry> wra = new WeightedRandomAllocator<HeatingSystemEntry>(Services.Rnd, MyLogger);

            var potentialRenovationHeatingSystems = srcHeatingSystems
                .Where(x => x.Age > 10 && x.HeatDemand > 0 && Math.Abs(x.HeatDemand - x.OriginalHeatDemand2017) < 0.1).ToList();
            if (potentialRenovationHeatingSystems.Count == 0) {
                throw new FlaException("Not a single heating system found");
            }

            double averageRenovationFactor = slice.AverageHouseRenovationFactor;
            if (Math.Abs(averageRenovationFactor) < 0.000001) {
                throw new FlaException("Renovation factor was 0 for scenario " + slice);
            }

            int numberOfBuildingRenovations = (int)(slice.RenovationRatePercentage * srcHouses.Count);
            Info("renovating houses, target number " + numberOfBuildingRenovations + " GWh, renovation factor " + averageRenovationFactor);
            bool failOnOversubscribe = slice.DstYear != 2050;
            var systemsToRenovate = wra.PickNumberOfObjects(potentialRenovationHeatingSystems,
                WeighingFunctionForRenovation,
                numberOfBuildingRenovations,
                failOnOversubscribe);
            Info("Renovating " + systemsToRenovate.Count + " houses with a total heat demand of " + systemsToRenovate.Sum(x => x.HeatDemand));
            foreach (var entry in systemsToRenovate) {
                entry.RenovateAllHeatDemand(averageRenovationFactor);
            }


            //change heating systems
            var changeableHeatingSystems = srcHeatingSystems.ToList();
            int elapsedTime = slice.DstYear - slice.PreviousSliceNotNull.DstYear;
            foreach (var heatingSystemEntry in srcHeatingSystems) {
                heatingSystemEntry.Age += elapsedTime;
            }


            int yearsToAge = slice.DstYear - slice.PreviousSliceNotNull.DstYear;
            RowCollection rc = new RowCollection("Changes", "Changes");
            double totalOilHeatDemand2017 = changeableHeatingSystems.Where(x => x.HeatingSystemType2017 == HeatingSystemType.Öl)
                .Sum(x => x.OriginalHeatDemand2017);
            double oilenergyAmountToChange = totalOilHeatDemand2017 * slice.Energy2017PercentageFromOilToHeatpump;
            ChangeHeatingSystemsForOneType(slice, changeableHeatingSystems, HeatingSystemType.Öl, oilenergyAmountToChange, rc, dstHousesByGuid);
            double totalGasHeatDemand2017 = changeableHeatingSystems.Where(x => x.HeatingSystemType2017 == HeatingSystemType.Gas)
                .Sum(x => x.OriginalHeatDemand2017);
            double gasEnergyAmountToChange = totalGasHeatDemand2017 * slice.Energy2017PercentageFromGasToHeatpump;
            ChangeHeatingSystemsForOneType(slice, changeableHeatingSystems, HeatingSystemType.Gas, gasEnergyAmountToChange, rc, dstHousesByGuid);
            double totalOtherHeatDemand = changeableHeatingSystems.Where(x => x.HeatingSystemType2017 == HeatingSystemType.Other)
                .Sum(x => x.OriginalHeatDemand2017);
            double otherDemandToChange = slice.Energy2017PercentageFromOtherToHeatpump * totalOtherHeatDemand;
            ChangeHeatingSystemsForOneType(slice, changeableHeatingSystems, HeatingSystemType.Other, otherDemandToChange, rc, dstHousesByGuid);
            var fn = MakeAndRegisterFullFilename("HeatingChangeLog.xlsx", slice);
            if (rc.Rows.Count > 0) {
                XlsxDumper.WriteToXlsx(fn, rc);
            }

            dbDstHouses.BeginTransaction();
            foreach (HeatingSystemEntry heatingSystemEntry in srcHeatingSystems) {
                heatingSystemEntry.Age += yearsToAge;
                heatingSystemEntry.ID = 0;

                dbDstHouses.Save(heatingSystemEntry);
            }

            dbDstHouses.CompleteTransaction();
            var srcHouseGuids = srcHouses.Select(x => x.Guid).ToHashSet();
            var dstHouseGuids = dstHouses.Select(x => x.Guid).ToHashSet();
            foreach (var heatingSystem in srcHeatingSystems) {
                srcHouseGuids.Should().Contain(heatingSystem.HouseGuid);
                dstHouseGuids.Should().Contain(heatingSystem.HouseGuid);
            }
        }

        private void ChangeHeatingSystemsForOneType([NotNull] ScenarioSliceParameters slice,
                                                    [NotNull] [ItemNotNull] List<HeatingSystemEntry> allPotentialSystemsToChange,
                                                    HeatingSystemType heatingSystemType,
                                                    double sumToSwitch,
                                                    [NotNull] RowCollection rc,
                                                    [NotNull] Dictionary<string, House> housesByGuid)
        {
            if (Math.Abs(sumToSwitch) < 0.1) {
                return;
            }

            var rb = RowBuilder.Start("Type to Change", heatingSystemType.ToString());
            rc.Add(rb);
            var matchingPotentialSystemsToChange =
                allPotentialSystemsToChange.Where(x => x.SynthesizedHeatingSystemType == heatingSystemType).ToList();
            rb.Add("Planned Sum", sumToSwitch);
            rb.Add("Energy Demand before across all systems", matchingPotentialSystemsToChange.Select(x => x.OriginalHeatDemand2017).Sum());
            WeightedRandomAllocator<HeatingSystemEntry> wra = new WeightedRandomAllocator<HeatingSystemEntry>(Services.Rnd, MyLogger);

            var pickedHeatingSystems = wra.PickObjectUntilLimit(matchingPotentialSystemsToChange,
                WeighingFunctionForSwitchingToHeatpump,
                x => x.OriginalHeatDemand2017,
                sumToSwitch,
                false);
            double changedEnergy = 0;
            foreach (var pickedHeatingSystem in pickedHeatingSystems) {
                pickedHeatingSystem.Age = 0;
                pickedHeatingSystem.SynthesizedHeatingSystemType = HeatingSystemType.Heatpump;
                pickedHeatingSystem.ProvideProfile = true;
                changedEnergy += pickedHeatingSystem.OriginalHeatDemand2017;
                House house = housesByGuid[pickedHeatingSystem.HouseGuid];
                var rb1 = RowBuilder.Start("House", house.ComplexName);
                rb1.Add("Changed Energy", pickedHeatingSystem.EffectiveEnergyDemand);
                rb1.Add("Heating System", heatingSystemType);
                rc.Add(rb1);
            }

            rb.Add("Changed Sum", changedEnergy);
            Info("Changed " + pickedHeatingSystems.Count + " from " + heatingSystemType + " to heatpump for a total of " + changedEnergy / 1_000_000 +
                 " gwh");
            double overSubscribed = sumToSwitch - changedEnergy;
            rb.Add("Oversubscribed", overSubscribed);
            if (slice.DstYear != 2050) {
                if (overSubscribed > 0) {
                    throw new FlaException("Problem: tried to allocate " + sumToSwitch / Constants.GWhFactor +
                                           "gwh to heat pumps, but could only switch " + changedEnergy / Constants.GWhFactor + " gwh in the year " +
                                           slice.DstYear + " and scenario " + slice.DstScenario + " from type " + heatingSystemType);
                }

                //im letzten jahr ist oversubscribe ok.
                //overSubscribed.Should().BeLessOrEqualTo(0);
            }

            var leftoveroldSystems = matchingPotentialSystemsToChange.Where(x => x.SynthesizedHeatingSystemType == heatingSystemType).ToList();
            rb.Add("Energy Demand after across all systems", leftoveroldSystems.Select(x => x.EffectiveEnergyDemand).Sum());
        }

        private void DumpHeatingSystemsToXlsx([NotNull] ScenarioSliceParameters slice,
                                              [NotNull] [ItemNotNull] List<HeatingSystemEntry> srcHeatingSystems,
                                              [NotNull] Dictionary<string, House> dstHousesByGuid)
        {
            RowCollection rc1 = new RowCollection("HeatingSystems", "HeatingSystems");
            foreach (var heatingSystemEntry in srcHeatingSystems) {
                var house = dstHousesByGuid[heatingSystemEntry.HouseGuid];
                var rb = RowBuilder.Start("House", house.ComplexName);
                rb.Add("Effective EnergyDemand", heatingSystemEntry.EffectiveEnergyDemand);
                rb.Add("Effective HeatDemand", heatingSystemEntry.HeatDemand);
                rb.Add("Heating system type", heatingSystemEntry.SynthesizedHeatingSystemType);
                rb.Add("Age", heatingSystemEntry.Age);
                rc1.Add(rb);
            }

            var fn1 = MakeAndRegisterFullFilename("heatingsystemdump.xlsx", slice);
            XlsxDumper.WriteToXlsx(fn1, rc1);
        }

        private static double WeighingFunctionForRenovation([NotNull] HeatingSystemEntry heatingSystemEntry)
        {
            double energyWeight = Math.Pow(heatingSystemEntry.CalculatedAverageHeatingEnergyDemandDensity, 2);

            double combinedWeight = energyWeight;
            if (double.IsInfinity(combinedWeight)) {
                throw new Exception("Invalid weight");
            }

            return combinedWeight;
        }

        private static double WeighingFunctionForSwitchingToHeatpump([NotNull] HeatingSystemEntry heatingSystemEntry)
        {
            double ageWeight = heatingSystemEntry.Age / 30.0;
            if (double.IsInfinity(ageWeight)) {
                throw new Exception("Invalid weight");
            }

            return ageWeight;
        }
    }
}
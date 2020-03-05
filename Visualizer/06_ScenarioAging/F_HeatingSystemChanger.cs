using System;
using System.Linq;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Visualisation.SingleSlice;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {
    // ReSharper disable once InconsistentNaming
    public class F_HeatingSystemChanger : RunableForSingleSliceWithBenchmark {
        public F_HeatingSystemChanger([NotNull] ServiceRepository services) : base(nameof(F_HeatingSystemChanger),
            Stage.ScenarioCreation, 600, services, false, new HeatingSystemCharts())
        {
            DevelopmentStatus.Add("//todo: are other heating systems being replaced too?");
            DevelopmentStatus.Add("implementation is missing");
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Services.SqlConnection.RecreateTable<HeatingSystemEntry>(Stage.Houses, parameters);
            var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters.PreviousScenarioNotNull).Database;
            var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var srcHeatingSystems = dbSrcHouses.Fetch<HeatingSystemEntry>();
            if (srcHeatingSystems.Count == 0) {
                throw new FlaException("No heating systems were found");
            }

            int elapsedTime = parameters.DstYear - parameters.PreviousScenarioNotNull.DstYear;
            foreach (var heatingSystemEntry in srcHeatingSystems) {
                heatingSystemEntry.Age += elapsedTime;
            }

            int yearsToAge = parameters.DstYear - parameters.PreviousScenarioNotNull.DstYear;
            var potentialSystemsToChange = srcHeatingSystems.Where(x => x.SynthesizedHeatingSystemType == HeatingSystemType.Öl || x.SynthesizedHeatingSystemType == HeatingSystemType.Gas).ToList();

            WeightedRandomAllocator<HeatingSystemEntry> wra = new WeightedRandomAllocator<HeatingSystemEntry>(Services.Rnd);

            double WeighingFunction(HeatingSystemEntry heatingSystemEntry)
            {
                double ageWeight = heatingSystemEntry.Age / 30.0;
                double energyWeight = 1 - heatingSystemEntry.AverageHeatingEnergyDemandDensity / 200;
                if (energyWeight <= 0) {
                    energyWeight = 0.000001;
                }

                double combinedWeight = 1 / (ageWeight * energyWeight);
                if (double.IsInfinity(combinedWeight)) {
                    throw new Exception("Invalid weight");
                }

                return combinedWeight;
            }
/*
            double SumFunction(HeatingSystemEntry heatingSystemEntry)
            {
                return heatingSystemEntry.YearlyEnergyDemand;
            }
            */
            var pickedHeatingSystems = wra.PickObjects(potentialSystemsToChange, WeighingFunction, (int)parameters.ConversionToHeatPumpNumber);

            if (potentialSystemsToChange.Count < parameters.ConversionToHeatPumpNumber) {
                throw new Exception("not enough other heating systems left for heat pump conversion demand");
            }

            foreach (var pickedHeatingSystem in pickedHeatingSystems) {
                pickedHeatingSystem.Age = 0;
                pickedHeatingSystem.SynthesizedHeatingSystemType = HeatingSystemType.Heatpump;
            }

            dbDstHouses.BeginTransaction();
            foreach (HeatingSystemEntry heatingSystemEntry in srcHeatingSystems) {
                heatingSystemEntry.Age += yearsToAge;
                heatingSystemEntry.HeatingSystemID = 0;
                dbDstHouses.Save(heatingSystemEntry);
            }

            dbDstHouses.CompleteTransaction();
        }
    }
}
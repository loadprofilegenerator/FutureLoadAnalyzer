using BurgdorfStatistics.Tooling;
using Common.Steps;
using JetBrains.Annotations;

namespace BurgdorfStatistics._06_ScenarioAging {
    // ReSharper disable once InconsistentNaming
    public class L_Airconditioning : RunableForSingleSliceWithBenchmark {
        public L_Airconditioning([NotNull] ServiceRepository services)
            : base(nameof(L_Airconditioning), Stage.ScenarioCreation, 1200, services, false)
        {
            DevelopmentStatus.Add("Not implemented");
        }


        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            /*
                        throw new NotImplementedException();
                        Services.SqlConnection.RecreateTable<HeatingSystemEntry>(Stage.Houses, up.DstScenario, up.DstYear);
                        var dbSrcHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, up.SrcScenario, up.SrcYear).Database;
                        var dbDstHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, up.DstScenario, up.DstYear).Database;
                        var srcHeatingSystems = dbSrcHouses.Fetch<HeatingSystemEntry>();
                        if (srcHeatingSystems.Count == 0)
                        {
                            throw new Exception("No cars were found");
                        }
            
                        int yearsToAge = up.DstYear - up.SrcYear;
                        var potentialSystemsToChange = srcHeatingSystems.Where(x =>
                            x.HeatingSystemType == HeatingSystemType.OilHeating ||
                            x.HeatingSystemType == HeatingSystemType.Gasheating).ToList();
                        List<G_DHWSystemChanger.RangeEntry> rangeEntries = new List<G_DHWSystemChanger.RangeEntry>();
                        foreach (HeatingSystemEntry heatingSystemEntry in potentialSystemsToChange)
                        {
                            G_DHWSystemChanger.RangeEntry re = new G_DHWSystemChanger.RangeEntry();
                            heatingSystemEntry.Age += yearsToAge;
                            double ageWeight = heatingSystemEntry.Age / 30.0;
                            double energyWeight = 1 - heatingSystemEntry.AverageHeatingEnergyDemandDensity / 200;
                            if (energyWeight <= 0)
                            {
                                energyWeight = 0.000001;
                            }
                            double combinedWeight = 1 / (ageWeight * energyWeight);
                            if (double.IsInfinity(combinedWeight))
                            {
                                throw new Exception("Invalid weight");
                            }
            
                            re.HeatingSystemEntry = heatingSystemEntry;
                            re.Weight = combinedWeight;
                            rangeEntries.Add(re);
                        }
            
                        if (potentialSystemsToChange.Count < up.ConversionToHeatPumpNumber)
                        {
                            throw new Exception("not enough other heating systems left for heat pump conversion demand");
                        }
                        double maxRangeValue = rangeEntries.Max(x => x.EndRange);
                        for (int i = 0; i < up.ConversionToHeatPumpNumber; i++)
                        {
                            double pick = Services.Rnd.NextDouble() * maxRangeValue;
                            var re = rangeEntries.Single(x => pick >= x.StartRange && pick <= x.EndRange);
                            re.HeatingSystemEntry.Age = 0;
                            re.HeatingSystemEntry.HeatingSystemType = HeatingSystemType.Heatpump;
                        }
                        dbDstHouses.BeginTransaction();
                        foreach (HeatingSystemEntry heatingSystemEntry in srcHeatingSystems)
                        {
                            heatingSystemEntry.HeatingSystemID = 0;
                            dbDstHouses.Save(heatingSystemEntry);
                        }
                        
                        dbDstHouses.CompleteTransaction();*/
        }
    }
}
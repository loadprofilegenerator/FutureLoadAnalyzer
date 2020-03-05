using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Database;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._06_ScenarioVisualizer {
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public class A03_HouseEnergyConsumption : RunnableForAllScenarioWithBenchmark {
        //stacked bar chart for each element
        public A03_HouseEnergyConsumption([NotNull] ServiceRepository services) : base(nameof(A03_HouseEnergyConsumption),
            Stage.ScenarioVisualisation,
            103,
            services,
            true)
        {
        }

        protected override void RunActualProcess([NotNull] [ItemNotNull] List<ScenarioSliceParameters> allSlices,
                                                 [NotNull] AnalysisRepository analysisRepo)
        {
            Info("starting to make house results");
            MultiyearTrend myt = new MultiyearTrend();
            foreach (var slice in allSlices) {
                var households = analysisRepo.GetSlice(slice).Fetch<Household>();
                var occupants = households.SelectMany(x => x.Occupants).ToList();

                HouseComponentRepository hcr = new HouseComponentRepository(analysisRepo, slice);
                var houses = analysisRepo.GetSlice(slice).Fetch<House>();
                houses[0].CollectHouseComponents(hcr);

                myt[slice].AddValue("Businesses", hcr.Businesses.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Haushalt Energieverbrauch [GWh]", hcr.Households.Sum(x => x.EffectiveEnergyDemand), DisplayUnit.GWh);
                myt[slice].AddValue("Business Energieverbrauch [GWh]", hcr.Businesses.Sum(x => x.EffectiveEnergyDemand), DisplayUnit.GWh);
                myt[slice].AddValue("Gebäudeinfrastruktur Energieverbrauch  [GWh]",
                    hcr.BuildingInfrastructures.Sum(x => x.EffectiveEnergyDemand),
                    DisplayUnit.GWh);
                myt[slice].AddValue("Wärmepumpen Energieverbrauch [GWh]",
                    hcr.HeatingSystemEntries.Where(x => x.SynthesizedHeatingSystemType == HeatingSystemType.Heatpump)
                        .Sum(x => x.EffectiveEnergyDemand),
                    DisplayUnit.GWh);
                myt[slice].AddValue("Klimatisierung Energieverbrauch [GWh]",
                    hcr.AirConditioningEntries.Sum(x => x.EffectiveEnergyDemand),
                    DisplayUnit.GWh);
                var dhws = analysisRepo.GetSlice(slice).Fetch<DHWHeaterEntry>();
                myt[slice].AddValue("Energiebedarf Warmwasserboiler [GWh]", dhws.Sum(x => x.EffectiveEnergyDemand), DisplayUnit.GWh);

                myt[slice].AddValue("PV Energie [Gwh]", hcr.PVSystems.Sum(x => x.EffectiveEnergyDemand), DisplayUnit.GWh);
                double distance = hcr.CarDistanceEntries.Sum(x => x.CommutingDistance + x.FreizeitDistance) / occupants.Count;
                myt[slice].AddValue("Wegedistanz / Person / Jahr", distance, DisplayUnit.Stk);
                myt[slice].AddValue("Car Distance Entries", hcr.CarDistanceEntries.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Anzahl Gebäudeinfrastruktur", hcr.BuildingInfrastructures.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Wasserkraftwerke", hcr.Wasserkraft.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Wasserkraftwerke Energie", hcr.Wasserkraft.Sum(x => x.EffectiveEnergyDemand), DisplayUnit.Stk);
                myt[slice].AddValue("Heizsysteme Anzahl", hcr.HeatingSystemEntries.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Heizsysteme Gesamtenergiebedarf [GWh]",
                    hcr.HeatingSystemEntries.Sum(x => x.EffectiveEnergyDemand),
                    DisplayUnit.GWh);
                myt[slice].AddValue("Heizsysteme Wärmepumpen Anzahl",
                    hcr.HeatingSystemEntries.Count(x => x.SynthesizedHeatingSystemType == HeatingSystemType.Heatpump),
                    DisplayUnit.Stk);
                myt[slice].AddValue("Klimatisierung Anzahl", hcr.AirConditioningEntries.Count, DisplayUnit.Stk);
                var cars = analysisRepo.GetSlice(slice).Fetch<Car>();
                myt[slice].AddValue("Autos gesamt", cars.Count, DisplayUnit.Stk);
                myt[slice].AddValue("Elektroautos", cars.Count(x => x.CarType == CarType.Electric), DisplayUnit.Stk);
                myt[slice].AddValue("DHW Elektrisch", dhws.Count(x => x.DhwHeatingSystemType == DhwHeatingSystem.Electricity), DisplayUnit.Stk);
                myt[slice].AddValue("DHW Heatpump", dhws.Count(x => x.DhwHeatingSystemType == DhwHeatingSystem.Heatpump), DisplayUnit.Stk);
            }

            var filename3 = MakeAndRegisterFullFilename("HouseEnergyResults.xlsx", Constants.PresentSlice);
            Info("Writing results to " + filename3);

            XlsxDumper.DumpMultiyearTrendToExcel(filename3, myt);
            SaveToArchiveDirectory(filename3, RelativeDirectory.Report, Constants.PresentSlice);
            SaveToPublicationDirectory(filename3, Constants.PresentSlice, "4.4");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data.DataModel.Dst;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._03_KomplexEnergy {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A_MergeKantonEnergieDatenToKomplexes : RunableWithBenchmark {
        protected override void RunChartMaking()
        {
        }

        protected override void RunActualProcess()
        {
            var dbcomplex = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var dbraw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbEnergy = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice);
            dbEnergy.RecreateTable<ComplexBuildingData>();
            var complexes = dbcomplex.Fetch<BuildingComplex>();
            var ebb = dbraw.Fetch<EnergiebedarfsdatenBern>();
            var cbds = new List<ComplexBuildingData>();
            dbEnergy.BeginTransaction();
            //this collects the data from the bern data to the complexes
            var newlycreatedBuildingData = 0;
            var mergedBuildingData = 0;
            foreach (var bern in ebb) {
                var komplex = complexes.Where(x => x.EGids.Contains(bern.egid)).ToList();
                if (komplex.Count != 1) {
                    throw new Exception("Too many komplexes for this egid");
                }

                var k = komplex[0];

                var cbd = cbds.FirstOrDefault(x => x.ComplexName == k.ComplexName);
                if (cbd == null) {
                    cbd = new ComplexBuildingData {
                        ComplexName = k.ComplexName
                    };
                    cbds.Add(cbd);
                    newlycreatedBuildingData++;
                }
                else {
                    mergedBuildingData++;
                }

                if (!string.IsNullOrWhiteSpace(bern.upd_gtyp)) {
                    cbd.GebäudeTypen.Add(bern.upd_gtyp);
                }

                cbd.NumberEnergieBernBuildings++;
                cbd.TotalArea += bern.garea;
                cbd.TotalEnergieBezugsfläche += bern.upd_ebf;
                cbd.AnzahlWohnungenBern += bern.ganzwhg;
                cbd.NumberOfMergedEntries++;
                cbd.calc_whzww += bern.calc_ehzww;
                cbd.calc_whzww += bern.calc_whzww;
                cbd.BuildingAges.Add(bern.gbauj);
                dbEnergy.Save(cbd);
            }

            Info( "newly created building entries: " + newlycreatedBuildingData);
            Info( "merged building entries: " + mergedBuildingData);

            dbEnergy.CompleteTransaction();
        }

        public A_MergeKantonEnergieDatenToKomplexes([NotNull] ServiceRepository services)
            : base(nameof(A_MergeKantonEnergieDatenToKomplexes), Stage.ComplexEnergyData, 1, services, true)
        {
        }
    }
}
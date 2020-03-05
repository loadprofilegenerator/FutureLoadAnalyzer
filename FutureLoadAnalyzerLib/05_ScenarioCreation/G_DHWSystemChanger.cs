using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class G_DHWSystemChanger : RunableForSingleSliceWithBenchmark {
        public G_DHWSystemChanger([NotNull] ServiceRepository services) : base(nameof(G_DHWSystemChanger),
            Stage.ScenarioCreation,
            700,
            services,
            false)
        {
            DevelopmentStatus.Add("//todo: are other heating systems being replaced too?");
            DevelopmentStatus.Add("This is not implemented");
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        protected override void RunActualProcess([NotNull] ScenarioSliceParameters slice)
        {
            var dbSrcHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice.PreviousSliceNotNull);
            var dbDstHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            dbDstHouses.RecreateTable<DHWHeaterEntry>();
            var srcdhwsystems = dbSrcHouses.Fetch<DHWHeaterEntry>();
            dbDstHouses.BeginTransaction();
            WeightedRandomAllocator<DHWHeaterEntry> dhwAllocator = new WeightedRandomAllocator<DHWHeaterEntry>(Services.Rnd, Services.Logger);
            int numberOfObjects = (int)slice.DHWSystemConversionNumber;
            var electricBoilers = srcdhwsystems.Where(x => x.DhwHeatingSystemType == DhwHeatingSystem.Electricity).ToList();
            if (electricBoilers.Count == 0 && numberOfObjects > 0) {
                throw new FlaException("No electric boilers left when trying to allocate " + numberOfObjects);
            }

            bool failOnOversubscribe = slice.DstYear != 2050;
            var systemsToChange = dhwAllocator.PickNumberOfObjects(srcdhwsystems, x => x.EffectiveEnergyDemand, numberOfObjects, failOnOversubscribe);
            foreach (var entry in systemsToChange) {
                entry.DhwHeatingSystemType = DhwHeatingSystem.Heatpump;
                entry.EffectiveEnergyDemand = entry.EffectiveEnergyDemand / 3;
            }

            foreach (var dhw in srcdhwsystems) {
                dhw.ID = 0;
                dbDstHouses.Save(dhw);
            }

            dbDstHouses.CompleteTransaction();
        }
    }
}
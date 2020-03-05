using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class S_CheckHeatingSystem : RunableWithBenchmark {
        //turn potential households into real households, filter by yearly consumption, turn the rest into building Infrastructure
        public S_CheckHeatingSystem([NotNull] ServiceRepository services)
            : base(nameof(S_CheckHeatingSystem), Stage.Houses, 1800, services, false)
        {
            DevelopmentStatus.Add("Make yearly gas use entries properly");
            DevelopmentStatus.Add("Make yearly fernwärme entries properly");
        }

        protected override void RunActualProcess()
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var ebbe = dbRaw.Fetch<EnergiebedarfsdatenBern>();
            RowCollection rc = new RowCollection( "Analysis", "Analysis");
            foreach (var house in houses) {
                RowBuilder rb = RowBuilder.Start("House", house.ComplexName );
                var ebbedata = ebbe.Where(x => house.EGIDs.Contains((int)x.egid)).ToList();
                foreach (var ebbeSet in ebbedata) {
                    ebbe.Remove(ebbeSet);
                }

                rb.Add("Ebbe", JsonConvert.SerializeObject(ebbedata));
                rc.Add(rb);
            }

            foreach (var ebbeset in ebbe) {
                RowBuilder rb = RowBuilder.Start("Ebbe eGid", ebbeset.egid);
                rb.Add("Ebbe", JsonConvert.SerializeObject(ebbeset));
                rc.Add(rb);
            }

            var fn = MakeAndRegisterFullFilename("HeatingSystemAnalysis.xlsx", Constants.PresentSlice);
            XlsxDumper.WriteToXlsx(fn, rc);
        }
    }
}
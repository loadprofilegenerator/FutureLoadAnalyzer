using System.Diagnostics.CodeAnalysis;
using System.IO;
using Common;
using Common.Steps;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [UsedImplicitly]
    public class A_LoadProfilePreparer : RunableForSingleSliceWithBenchmark {

        public A_LoadProfilePreparer([NotNull] ServiceRepository services)
            : base(nameof(A_LoadProfilePreparer), Stage.ProfileGeneration, 100, services, false, null)
        {
        }
        protected override void RunActualProcess(ScenarioSliceParameters slice)
        {
            var fn = Path.Combine(FilenameHelpers.GetTargetDirectory(MyStage, SequenceNumber, Name, slice,Services.RunningConfig), "Export");
            HouseProcessor hp = new HouseProcessor(Services,fn, MyStage);
            hp.ProcessAllHouses(slice,MakeAndRegisterFullFilename,
                HouseProcessor.ProcessingMode.Preparing, DevelopmentStatus);
        }

    }
}

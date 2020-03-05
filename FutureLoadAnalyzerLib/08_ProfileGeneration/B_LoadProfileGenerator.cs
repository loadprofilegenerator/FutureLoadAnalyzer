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
    public class B_LoadProfileGenerator : RunableForSingleSliceWithBenchmark {
        public const int MySequenceNumber = 200;
        public B_LoadProfileGenerator([NotNull] ServiceRepository services)
            : base(nameof(B_LoadProfileGenerator), Stage.ProfileGeneration, MySequenceNumber, services, false, null)
        {
        }
        protected override void RunActualProcess(ScenarioSliceParameters slice)
        {
            if (Services.RunningConfig.OnlyPrepareProfiles) {
                return;
            }
            var fn = Path.Combine(FilenameHelpers.GetTargetDirectory(MyStage, SequenceNumber, Name, slice,Services.RunningConfig), "Export");
            HouseProcessor hp = new HouseProcessor(Services,fn, MyStage);
            hp.ProcessAllHouses(slice,MakeAndRegisterFullFilename,
                HouseProcessor.ProcessingMode.Collecting, DevelopmentStatus);
        }

    }
}

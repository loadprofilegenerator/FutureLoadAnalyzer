using System;
using JetBrains.Annotations;
using PowerArgs;

namespace Fla {
    public enum StagesToExecute {
        RawToHouses,
        Profiles,
        Scenarios,
    }

    [UsedImplicitly]
    public class ExampleArgs {
        [ArgRequired]
        [ArgDescription("Filename to write to")]
        [ArgShortcut("-f")]
        [CanBeNull]
        public string Filename { get; set; }
    }

    [UsedImplicitly]
    public class RunArgument {
        [ArgRequired]
        [ArgDescription("Filename of Config file")]
        [CanBeNull]
        public string Filename { get; set; }
    }

    public static class Program {
        // ReSharper disable once UnusedParameter.Local
        public static void Main([CanBeNull] [ItemCanBeNull] string[] args)
        {
            try {
                Args.InvokeAction<FlaProgram>(args);
            }
            catch (ArgException ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
using System.Collections.Generic;
using JetBrains.Annotations;
using PowerArgs;

namespace Fla {
    public class ExecutionArgs {
        [CanBeNull]
        public List<StagesToExecute> Stages { get; set; } = new List<StagesToExecute>();
        [CanBeNull]
        [ItemNotNull]
        public List<string> Scenarios { get; set; } = new List<string>();
        [CanBeNull]
        public List<int> Years { get; set; } = new List<int>();
        [ArgDescription("not used, just for displaying stages")]
        public StagesToExecute DummyStage { get; set; }
    }
}
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._02_Komplexes {
    public class ComplexesToMerge {
        public ComplexesToMerge([NotNull] string complexName1, [NotNull] string complexName2)
        {
            ComplexName1 = complexName1;
            ComplexName2 = complexName2;
        }

        [NotNull]
        public string ComplexName1 { get; }
        [NotNull]
        public string ComplexName2 { get; }
        public bool IsProcessed { get; set; }
        public override string ToString() => ComplexName1 + " / " + ComplexName2 + " : " + IsProcessed;
    }
}
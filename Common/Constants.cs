using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Common.Steps;
using JetBrains.Annotations;

namespace Common {
    public enum RelativeDirectory {
        Generation,
        Load,
        Report,
        Trafokreise,
        Abschlussbericht,
        Bruno
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class Constants {
        public const double GWhFactor = 1_000_000;

        [NotNull]
        public static RGB Black { get; } = new RGB(0, 0, 0);

        [NotNull]
        public static RGB Blue { get; } = new RGB(0, 0, 255);

        [NotNull]
        public static string CalculationProfilerJson { get; set; } = "calcprofiler.json";

        [NotNull]
        public static RGB Green { get; } = new RGB(0, 255, 0);

        [NotNull]
        public static RGB LightBlue { get; } = new RGB(128, 128, 255);

        [NotNull]
        public static RGB LightGreen { get; } = new RGB(128, 255, 128);

        public static bool MakeDummyProfilesOnly { get; set; } = false;

        [NotNull]
        public static object MyGlobalLock { get; set; } = new object();

        [NotNull]
        public static RGB Orange { get; } = new RGB(255, 128, 0);

        [NotNull]
        public static ScenarioSliceParameters PresentSlice { get; } =
            new ScenarioSliceParameters(Scenario.FromEnum(ScenarioEnum.Present), 2017, null);

        [NotNull]
        public static RGB Red { get; } = new RGB(255, 0, 0);

        [NotNull]
        public static RGB Türkis { get; } = new RGB(0, 255, 255);

        [NotNull]
        public static RGB Yellow { get; } = new RGB(255, 210, 0);

        public static bool ScrambledEquals<T>([ItemNotNull] [NotNull] IEnumerable<T> list1, [ItemNotNull] [NotNull] IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (var s in list1) {
                if (cnt.ContainsKey(s)) {
                    cnt[s]++;
                }
                else {
                    cnt.Add(s, 1);
                }
            }

            foreach (var s in list2) {
                if (cnt.ContainsKey(s)) {
                    cnt[s]--;
                }
                else {
                    return false;
                }
            }

            return cnt.Values.All(c => c == 0);
        }
    }
}
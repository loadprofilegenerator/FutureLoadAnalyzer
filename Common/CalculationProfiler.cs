using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Common {
    public class CalculationProfiler : ICalculationProfiler {
        public CalculationProfiler()
        {
            MainPart = new ProgramPart(null, "Main");
            Current = MainPart;
        }

        [UsedImplicitly]
        [NotNull]
        public ProgramPart MainPart { get; private set; }

        [CanBeNull]
        private ProgramPart Current { get; set; }

        public void StartPart([NotNull] string key)
        {
            if (Current == null) {
                throw new FlaException("Current was null");
            }

            if (Current.Key == key) {
                throw new FlaException("The current key is already " + key + ". Copy&Paste error?");
            }

            var newCurrent = new ProgramPart(Current, key);
            Current.Children.Add(newCurrent);
            Current = newCurrent;
        }

        public void StopPart([NotNull] string key)
        {
            if (Current == null) {
                throw new FlaException("Current was null");
            }

            if (Current.Key != key) {
                throw new FlaException("Mismatched key: Current: " + Current.Key + " trying to stop: " + key);
            }

            if (Current == MainPart) {
                throw new FlaException("Trying to stop the main");
            }

            Current.Stop = DateTime.Now;

            Console.WriteLine("Finished " + key + " after " + Current.Duration.ToString());

            Current = Current.Parent;
        }

        public void LogToConsole()
        {
            if (Current != MainPart) {
                throw new FlaException("Forgot to close: " + Current?.Key);
            }

            MainPart.Stop = DateTime.Now;
            LogOneProgramPartToConsole(MainPart, 0);
        }

        [NotNull]
        public static CalculationProfiler Read([NotNull] string path)
        {
            var dstPath = Path.Combine(path, Constants.CalculationProfilerJson);
            string json;
            using (var sw = new StreamReader(dstPath)) {
                json = sw.ReadToEnd();
            }

            var o = JsonConvert.DeserializeObject<CalculationProfiler>(json);
            return o;
        }

        public void WriteJson([NotNull] StreamWriter sw)
        {
            MainPart.Stop = DateTime.Now;
            var json = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Include
            });
            using (sw) {
                sw.WriteLine(json);
            }
        }

        private static void LogOneProgramPartToConsole([NotNull] ProgramPart part, int level)
        {
            var padding = "";
            for (var i = 0; i < level; i++) {
                padding += "  ";
            }

            Console.WriteLine(padding + part.Key + "\t" + part.Duration.TotalSeconds);
            foreach (var child in part.Children) {
                LogOneProgramPartToConsole(child, level + 1);
            }
        }

#pragma warning disable CA1034 // Nested types should not be visible
        public class ProgramPart {
#pragma warning restore CA1034 // Nested types should not be visible
            public ProgramPart([CanBeNull] ProgramPart parent, [NotNull] string key)
            {
                Parent = parent;
                Key = key;
                Start = DateTime.Now;
            }

            [NotNull]
            [ItemNotNull]
            public List<ProgramPart> Children { get; } = new List<ProgramPart>();

            public TimeSpan Duration => Stop - Start;

            public double Duration2 { get; set; }

            [UsedImplicitly]
            [NotNull]
            public string Key { get; set; }

            [JsonIgnore]
            [CanBeNull]
            public ProgramPart Parent { get; }

            [UsedImplicitly]
            public DateTime Start { get; set; }

            [UsedImplicitly]
            public DateTime Stop { get; set; }

            [NotNull]
            public override string ToString() => Key + " - " + Duration;
        }
    }
}

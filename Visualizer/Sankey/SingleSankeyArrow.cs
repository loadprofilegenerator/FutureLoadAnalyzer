using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Common;
using Common.Config;
using Common.Steps;
using JetBrains.Annotations;

namespace Visualizer.Sankey {
    public class SingleSankeyArrow : BasicLoggable {
        [NotNull] private readonly string _className;
        private readonly int _sequenceNumber;
        [NotNull] private readonly IServiceRepository _services;

        [NotNull] private readonly ScenarioSliceParameters _slice;
        private readonly Stage _stage;

        public SingleSankeyArrow([NotNull] string arrowName,
                                 int trunkLength,
                                 Stage stage,
                                 int sequenceNumber,
                                 [NotNull] string className,
                                 [NotNull] ScenarioSliceParameters slice,
                                 [NotNull] IServiceRepository services) : base(services.Logger, stage, nameof(SingleSankeyArrow))
        {
            _slice = slice;
            _services = services;
            _stage = stage;
            _sequenceNumber = sequenceNumber;
            _className = className;
            ArrowName = arrowName.Replace(" ", "_");
            TrunkLength = trunkLength;
        }

        [NotNull]
        public string ArrowName { get; }

        public int TrunkLength { get; }

        [ItemNotNull]
        [NotNull]
        private List<SankeyEntry> Entries { get; } = new List<SankeyEntry>();

        public void AddEntry([NotNull] SankeyEntry entry)
        {
            Entries.Add(entry);
        }

        [NotNull]
        public string FullPngFileName()
        {
            var targetdir = FilenameHelpers.GetTargetDirectory(_stage, _sequenceNumber, _className, _slice, _services.RunningConfig);
            return Path.Combine(targetdir, ArrowName + ".png");
        }

        [NotNull]
        public string FullPyFileName()
        {
            var targetdir = FilenameHelpers.GetTargetDirectory(_stage, _sequenceNumber, _className, _slice, _services.RunningConfig);
            return Path.Combine(targetdir, ArrowName + ".py");
        }

        [NotNull]
        public string FullTargetDirectory() =>
            FilenameHelpers.GetTargetDirectory(_stage, _sequenceNumber, _className, _slice, _services.RunningConfig);

        [NotNull]
        public string GetDirections()
        {
            var s = "";
            var builder = new StringBuilder();
            builder.Append(s);
            foreach (var entry in Entries) {
                builder.Append((int)entry.Orientation + ", ");
            }

            s = builder.ToString();
            return s.Substring(0, s.Length - 2);
        }

        [NotNull]
        public string GetFlows()
        {
            var s = "";
            var builder = new StringBuilder();
            builder.Append(s);
            foreach (var entry in Entries) {
                builder.Append(entry.Value.ToString(CultureInfo.InvariantCulture) + ", ");
            }

            s = builder.ToString();

            var sum = Entries.Select(x => x.Value).Sum();
            if (Math.Abs(sum) > 1) {
                foreach (var entry in Entries) {
                    Info(entry.Name + ": " + entry.Value);
                }

                throw new FlaException("Sankey does not add up to 0");
            }

            return s.Substring(0, s.Length - 2);
        }

        [NotNull]
        public string GetNames()
        {
            var s = "";
            var builder = new StringBuilder();
            builder.Append(s);
            foreach (var entry in Entries) {
                builder.Append("'" + entry.Name + "', ");
            }

            s = builder.ToString();
            return s.Substring(0, s.Length - 2);
        }

        [NotNull]
        public string GetPathLengths()
        {
            var s = "";
            var builder = new StringBuilder();
            builder.Append(s);
            foreach (var entry in Entries) {
                builder.Append(entry.PathLength.ToString(CultureInfo.InvariantCulture) + ", ");
            }

            s = builder.ToString();
            return s.Substring(0, s.Length - 2);
        }
    }
}
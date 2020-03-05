using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Config;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class BusinessProfileOverrideRepository {
        [NotNull] [ItemNotNull] private readonly List<BusinessProfileOverrideEntry> _overrides;

        public BusinessProfileOverrideRepository([NotNull] RunningConfig config) => _overrides = ReadEntries(config);

        public void CheckIfAllAreUsed([NotNull] [ItemNotNull] List<BusinessProfileOverrideEntry> used)
        {
            List<BusinessProfileOverrideEntry> missed = new List<BusinessProfileOverrideEntry>();
            foreach (var entry in _overrides) {
                if (!used.Contains(entry)) {
                    missed.Add(entry);
                }
            }

            if (missed.Count > 0) {
                string s = "Missed:  " + missed.Count + " / " + _overrides.Count + "\n" +
                           string.Join("\n", missed.Select(x => x.BusinessName + " " + x.Standort));
                throw new FlaException(s);
            }
        }

        [CanBeNull]
        public BusinessProfileOverrideEntry GetEntry([NotNull] string housename, [NotNull] string businessname, [NotNull] string standort)
        {
            var ore = _overrides.SingleOrDefault(x => x.HouseName == housename && x.Standort == standort && x.BusinessName == businessname);
            if (ore == null) {
                return null;
            }

            return ore;
        }

        [NotNull]
        [ItemNotNull]
        public List<BusinessProfileOverrideEntry> ReadEntries([NotNull] RunningConfig config)
        {
            string path = Path.Combine(config.Directories.BaseUserSettingsDirectory, "BusinessProfileOverrides.xlsx");
            var p = new ExcelPackage(new FileInfo(path));
            var ws = p.Workbook.Worksheets[1];
            int row = 2;
            var ores = new List<BusinessProfileOverrideEntry>();
            while (ws.Cells[row, 1].Value != null) {
                string houseName = (string)ws.Cells[row, 1].Value;
                string businessName = (string)ws.Cells[row, 2].Value;
                string standort = (string)ws.Cells[row, 3].Value;
                string profileName = (string)ws.Cells[row, 4].Value;
                BusinessProfileOverrideEntry ore = new BusinessProfileOverrideEntry(houseName, businessName, standort, profileName);
                ores.Add(ore);
                row += 1;
            }

            p.Dispose();
            return ores;
        }
    }
}
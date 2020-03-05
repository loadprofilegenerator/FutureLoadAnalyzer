using System;
using System.Collections.Generic;
using System.IO;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    public class OverrideRepository {
        [NotNull]
        [ItemNotNull]
        public List<OverrideEntry> ReadEntries([NotNull] ServiceRepository services)
        {
            string path = Path.Combine(services.RunningConfig.Directories.BaseUserSettingsDirectory, "HeatingSystemOverrides.xlsx");
            var p = new ExcelPackage(new FileInfo(path));
            var ws = p.Workbook.Worksheets[1];
            int row = 2;
            List<OverrideEntry> ores = new List<OverrideEntry>();
            while (ws.Cells[row, 1].Value != null) {
                string name = (string)ws.Cells[row, 1].Value;
                string heatingSystemTypeStr = (string)ws.Cells[row, 2].Value;
                HeatingSystemType hst = (HeatingSystemType)Enum.Parse(typeof(HeatingSystemType), heatingSystemTypeStr);
                string energyDemandSourceStr = (string)ws.Cells[row, 3].Value;
                EnergyDemandSource eds = (EnergyDemandSource)Enum.Parse(typeof(EnergyDemandSource), energyDemandSourceStr);
                double amount = (double)ws.Cells[row, 4].Value;
                OverrideEntry ore = new OverrideEntry(name, hst, eds, amount);
                ores.Add(ore);
                row += 1;
            }

            p.Dispose();
            return ores;
        }
    }
}
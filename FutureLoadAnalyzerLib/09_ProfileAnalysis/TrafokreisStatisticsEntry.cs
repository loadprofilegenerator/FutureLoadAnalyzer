using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._09_ProfileAnalysis {
    public class TrafokreisStatisticsEntry {
        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public TrafokreisStatisticsEntry()
        {
        }

        public TrafokreisStatisticsEntry([NotNull] string name) => Name = name;

        [NotNull]
        public string Name { get; set; }
        public double OriginalElectricityUse { get; set; }
        public double OriginalElectricityUseDay { get; set; }
        public double OriginalElectricityUseNight { get; set; }
        public int OriginalHouses { get; set; }
        public double ProfileElectricityUse { get; set; }
        public int ProfileHouses { get; set; }
        public double CollectedEnergyFromHouses { get; set; }
        public double ProfileDuringNight { get; set; }
        public double ProfileDuringDay { get; set; }

        public static void WriteHeaderToWs([NotNull] ExcelWorksheet ws)
        {
            int col = 1;
            ws.Cells[1, col++].Value = "Name";
            ws.Cells[1, col++].Value = "Original Houses";
            ws.Cells[1, col++].Value = "Exported Houses";
            ws.Cells[1, col++].Value = "Original Energy";
            ws.Cells[1, col++].Value = "Exported Energy";
            ws.Cells[1, col++].Value = "Planned in TKResults";
            ws.Cells[1, col++].Value = "Original Night Use";
            ws.Cells[1, col++].Value = "Original Day Use";
            ws.Cells[1, col++].Value = "Profile Night Use";
            // ReSharper disable once RedundantAssignment
            ws.Cells[1, col++].Value = "Profile Day Use";
        }

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void WriteToWorksheet([NotNull] ExcelWorksheet ws, int row)
        {
            int col = 1;
            ws.Cells[row, col++].Value = Name;
            ws.Cells[row, col++].Value = OriginalHouses;
            ws.Cells[row, col++].Value = ProfileHouses;
            ws.Cells[row, col++].Value = OriginalElectricityUse;
            ws.Cells[row, col++].Value = ProfileElectricityUse;
            ws.Cells[row, col++].Value = CollectedEnergyFromHouses;
            ws.Cells[row, col++].Value = OriginalElectricityUseNight;
            ws.Cells[row, col++].Value = OriginalElectricityUseDay;
            ws.Cells[row, col++].Value = ProfileDuringNight;
            ws.Cells[row, col++].Value = ProfileDuringDay;
            ws.Cells[row, col++].Value = ProfileDuringDay;
        }
    }
}
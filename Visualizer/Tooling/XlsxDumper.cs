using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BurgdorfStatistics.Tooling.Database;
using Common;
using Data.Database;
using JetBrains.Annotations;
using OfficeOpenXml;
using Xunit;

namespace BurgdorfStatistics.Tooling
{
    public class XlsxDumperTest {
        [Fact]
        public void RunTest()
        {
            RowCollection rc = new RowCollection();
            var rb =RowBuilder.Start("t1","mytxt").Add("blub", 1).Add("blub2", 1);
            rc.Add(rb);
            var rb2 = RowBuilder.Start("t1", "mytxt").Add("blub", 1).Add("blub5", 1);
            rc.Add(rb2);
            XlsxDumper.WriteToXlsx(rc,"v:\\t.xlsx","mysheet");
        }
    }
    public class XlsxDumper {
        public static void WriteToXlsx([NotNull] RowCollection rc, [NotNull] string fileName, [NotNull] string name)
        {
            if (rc.Rows.Count == 0) {
                throw new FlaException("Not a single row to export. This is probably not intended.");
            }
            if (File.Exists(fileName)) {
                File.Delete(fileName);
                Thread.Sleep(250);
            }
            var p = new ExcelPackage(new FileInfo(fileName));

            ExcelWorksheet ws = p.Workbook.Worksheets.Add(name);
            List<string> keys = rc.Rows.SelectMany(x => x.Values.Keys).Distinct().ToList();
            Dictionary<string, int> colidxByKey = new Dictionary<string, int>();
            for (int i = 0; i < keys.Count; i++) {
                colidxByKey.Add(keys[i], i+1);
                ws.Cells[1, i+1].Value = keys[i];
            }

            int rowIdx = 2;
            foreach (Row row in rc.Rows) {
                foreach (var pair in row.Values) {
                    int col = colidxByKey[pair.Key];
                    ws.Cells[rowIdx, col].Value = pair.Value;
                }

                rowIdx++;
            }

            p.Save();
            p.Dispose();
        }
    }
}

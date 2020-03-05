using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using BurgdorfStatistics.Tooling.Database;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.Export;
using JetBrains.Annotations;

namespace BurgdorfStatistics._10_ProfileExport {
    /// <summary>
    ///     export the profiles
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class Z10_CSVExporterLoad: RunableForSingleSliceWithBenchmark {
        public Z10_CSVExporterLoad([NotNull] ServiceRepository services)
            : base(nameof(Z10_CSVExporterLoad), Stage.ProfileExport, 1000, services, false)
        {
        }

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            //profile export
            var dbProfileExport =
                Services.SqlConnection.GetDatabaseConnection(Stage.ProfileExport, parameters);
            var prosumers = Prosumer.LoadProsumers(dbProfileExport, TableType.HouseLoad);

            var resultPathLoad = Path.Combine(dbProfileExport.GetResultFullPath(SequenceNumber, Name), "Export");
            if (Directory.Exists(resultPathLoad))
            {
                Directory.Delete(resultPathLoad, true);
                Thread.Sleep(500);
            }

            if (!Directory.Exists(resultPathLoad)) {
                Directory.CreateDirectory(resultPathLoad);
            }
            HashSet<string> usedKeys = new HashSet<string>();
            var trafokreise = prosumers.Select(x => x.TrafoKreis).Distinct().ToList();
            RowCollection rc = new RowCollection();
            foreach (var trafokreis in trafokreise) {
                if (string.IsNullOrWhiteSpace(trafokreis)) {
                    continue;
                }

                var filteredProsumers = prosumers.Where(x => x.TrafoKreis == trafokreis).ToList();
                string tkFileName = trafokreis.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue");
                var csvFileNameLoad = Path.Combine(resultPathLoad, tkFileName + ".csv");
                var sw2 = new StreamWriter(csvFileNameLoad);

                int lines = 0;
                foreach (var prosumer in filteredProsumers) {
                    var row = RowBuilder.Start("Trafokreis", trafokreis).Add("Name", prosumer.Name).Add("Energy", prosumer.SumElectricityFromProfile);
                    if (usedKeys.Contains(prosumer.HausanschlussKey)) {
                        throw new FlaException("This key was already exported");

                    }
                    usedKeys.Add(prosumer.HausanschlussKey);
                    //var hee = new HouseExportEntry(prosumer.Name, trafokreis, (int)prosumer.Isn, prosumer.HouseGuid);
                    //hee.Prosumers.Add(prosumer);
                    sw2.WriteLine( prosumer.GetCSVLine());
                    lines++;
                    rc.Add(row);
                }
                Log(MessageType.Info, "Wrote" + lines+ " lines to  " + csvFileNameLoad);
                sw2.Close();
            }
            var fn = MakeAndRegisterFullFilename("Load.xlsx", parameters);
            XlsxDumper.WriteToXlsx(rc, fn, "Loads");
        }
    }

}
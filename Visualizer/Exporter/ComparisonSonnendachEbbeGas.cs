using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace BurgdorfStatistics.Exporter
{
    public class ComparisonSonnendachEbbeGas : RunableWithBenchmark
    {

        public ComparisonSonnendachEbbeGas([NotNull] ServiceRepository services) : base(nameof(ComparisonSonnendachEbbeGas),
             Stage.ValidationExporting,10,services,false)
        {
            //DevelopmentStatus.Add("");
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum Columns {
            ComplexName,
            EGids,
            LocalnetISNIds,
            Adressen,
            Energiebezugsfläche,
           // EBHZWW,
            EBBE_calc_ehzww,
            Localnet_Gas,
            Localnet_Wärme,
            BFH_SonnendachHeizung,

        }

        protected override void RunActualProcess()
        {
            //string excelFileName = @"U:\SimZukunft\RawDataForMerging\DatenfürEnergiebilanzBurgdorf2018.xlsx";
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw,Constants.PresentSlice).Database;
            var dbComplexes = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;

            var complexes = dbComplexes.Fetch<BuildingComplex>();
            //var gwr = dbRaw.Fetch<GwrData>();
            var kanton = dbRaw.Fetch<EnergiebedarfsdatenBern>();
            var complexEnergy = dbComplexEnergy.Fetch<MonthlyElectricityUsePerStandort>();
            var houses = dbHouses.Fetch<House>();
            //var heatingSystems = dbHouses.Fetch<HeatingSystemEntry>();
            //var pvSystemEntries = dbHouses.Fetch<PvSystemEntry>();
            //var occupants = dbHouses.Fetch<Occupant>();
            //var businessEntries = dbHouses.Fetch<BusinessEntry>();
            //var households = dbHouses.Fetch<Household>();
            var sonnendach = dbRaw.Fetch<SonnenDach>();
            using (var p = new ExcelPackage())
            {
                //A workbook must have at least on cell, so lets add one...
                var ws = p.Workbook.Worksheets.Add("MySheet");
                //To set values in the spreadsheet use the Cells indexer.
                var columnNumbers = MakeColumnNumberDictionary();
                //header
                foreach (KeyValuePair<Columns, int> pair in columnNumbers) {
                    ws.Cells[1, pair.Value].Value = pair.Key.ToString();
                }

                int row = 2;
                foreach (var house in houses) {
                    BuildingComplex complex = complexes.Single(x => x.ComplexName == house.ComplexName);
                    //var heatingSystem = heatingSystems.Single(x => x.HouseGuid == house.HouseGuid);
                    //var pvSystem = pvSystemEntries.FirstOrDefault(x => x.HouseGuid == house.HouseGuid);
                    //var houseOccupants = occupants.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                    //var houseBusinessEntries = businessEntries.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                    //var houseHouseholds = households.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                    WriteOneLine(ws, row, columnNumbers, house,  kanton, //gwr,
                        complexEnergy, complex,sonnendach);
                    row++;
                }
                //Save the new workbook. We haven't specified the filename so use the Save as method.
                var filename = MakeAndRegisterFullFilename("ComparisonEbbeSonnendachLocalnet.xlsx", Name, "", Constants.PresentSlice);
                p.SaveAs(new FileInfo(filename));
                Log(MessageType.Warning, "File: " + filename);

            }
        }

        [NotNull]
        public string CleanJson([NotNull] string s)
        {
            return s.Replace("[", "").Replace("]", "");
        }
        // ReSharper disable once FunctionComplexityOverflow
        private void WriteOneLine([NotNull] ExcelWorksheet ws, int row, [NotNull] Dictionary<Columns, int> columnNumbers,
                                  [NotNull] House house, //[ItemNotNull] [NotNull] List<GwrData> gwr,
                                  [ItemNotNull] [NotNull] List<EnergiebedarfsdatenBern> kanton,
                                  [ItemNotNull] [NotNull] List<MonthlyElectricityUsePerStandort> complexEnergy,
                                  [NotNull] BuildingComplex mycomplex,
                                  //[NotNull] List<Household> households,
                                  [NotNull] [ItemNotNull] List<SonnenDach> sonnendachEntries
            )
        {
            ws.Cells[row, columnNumbers[Columns.ComplexName]].Value = house.ComplexName;
            ws.Cells[row, columnNumbers[Columns.Adressen]].Value = mycomplex.AdressesAsJson;
            ws.Cells[row, columnNumbers[Columns.EGids]].Value = CleanJson(mycomplex.EGIDsAsJson);
            ws.Cells[row, columnNumbers[Columns.LocalnetISNIds]].Value = CleanJson(mycomplex.GebäudeObjectIDsAsJson);
            /*var gwrs = gwr.Where(x => {Debug.Assert(x.EidgGebaeudeidentifikator_EGID != null, "x.EidgGebaeudeidentifikator_EGID != null");
                return mycomplex.EGids.Contains((long)x.EidgGebaeudeidentifikator_EGID);
            }).ToList();*/

            var ebbes = kanton.Where(x => mycomplex.EGids.Contains(x.egid)).ToList();
            ws.Cells[row, columnNumbers[Columns.EBBE_calc_ehzww]].Value = ebbes.Sum(x => x.calc_ehzww);
            ws.Cells[row, columnNumbers[Columns.Energiebezugsfläche]].Value = ebbes.Sum(x => x.upd_ebf);

            //energieträger aus ebbe
            //var heizträgerlst = ebbes.Select(x => x.upd_genhz).ToList();
            //ws.Cells[row, columnNumbers[Columns.EBHZ_GZ]].Value = ebbes.Where(x=> x.upd_genhz == 7203).Sum(x => x.calc_ehz);
            //ws.Cells[row, columnNumbers[Columns.EBWW_GZ]].Value = ebbes.Where(x => x.upd_genhz == 7203).Sum(x => x.calc_ehz);
            //ws.Cells[row, columnNumbers[Columns.EBHZWW]].Value = ebbes.Where(x => x.upd_genhz == 7203).Sum(x => x.calc_ehz);
            //warmwasser aus Ebbe
            //ws.Cells[row, columnNumbers[Columns.EBWW_GZ]].Value = ebbes.Where(x => x.upd_genww == 7203).Sum(x => x.calc_eww);
            // ws.Cells[row, columnNumbers[Columns.Energiebezugsfläche]].Value = ebbes.Sum(x=> x)
            var monthlies = complexEnergy.Where(x => mycomplex.CleanedStandorte.Contains(x.CleanedStandort)).ToList();
            ws.Cells[row, columnNumbers[Columns.Localnet_Gas]].Value =monthlies.Sum(x => x.YearlyGasUse) ;
            ws.Cells[row, columnNumbers[Columns.Localnet_Wärme]].Value =monthlies.Sum(x => x.YearlyFernwaermeUse);
            var sd = sonnendachEntries.Where(x => mycomplex.EGids.Contains(x.gwr_egid)).ToList();
            double sum = sd.Sum(x => x.bedarf_heizung + x.bedarf_warmwasser);
            ws.Cells[row, columnNumbers[Columns.BFH_SonnendachHeizung]].Value = sum;
        }



        [NotNull]
        private static Dictionary<Columns, int> MakeColumnNumberDictionary()
        {
            Dictionary<Columns, int> columnNumbers = new Dictionary<Columns, int>();
            int column = 1;
            foreach (Columns value in Enum.GetValues(typeof(Columns))) {
                columnNumbers.Add(value, column++);
            }

            return columnNumbers;
        }
    }
}

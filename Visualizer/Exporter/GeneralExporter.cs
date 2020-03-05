using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using BurgdorfStatistics._04_HouseMaker;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace BurgdorfStatistics.Exporter {
    public class GeneralExporter : RunableWithBenchmark {
        public GeneralExporter([NotNull] ServiceRepository services)
            : base(nameof(GeneralExporter), Stage.ValidationExporting, 1, services, false)
        {
            DevelopmentStatus.Add("Gas Gewerbe richtig reinmachen");
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum Columns {
            ComplexName,
            EGids,
            LocalnetISNIds,
            Adressen,
            WG_GWR_GebäudeAnzahl,
            WG_GEAK_Anzahl,
            EBBE_GebäudeTyp,
            GeoKoordinaten,
            GWR_WG,
            EBBE_GArea,
            EBBE_EBF_HZ_Updated,
            EBBE_calc_ehzww,
            EBBE_calc_ehz,
            EBBE_calc_eww,
            EBBE_EnergieträgerHz,
            EBBE_EnergieträgerWW,
            EBHZ_U,
            EBHZ_OL,
            EBHZ_KO,
            EBHZ_GZ,
            EBHZ_EL,
            EBHZ_HO,
            EBHZ_WP,
            EBHZ_SO,
            EBHZ_FW,
            EBHZ_A,
            EBWW_U,
            EBWW_OL,
            EBWW_KO,
            EBWW_GZ,
            EBWW_EL,
            EBWW_HO,
            EBWW_WP,
            EBWW_SO,
            EBWW_FW,
            EBWW_A,
            EBELS2S3,
            EBELS2,
            EBELS3,
            EBTHS2S3,
            EBTHS2,
            EBTHS3,
            EBS2S3,
            EBS2,
            EBS3,
            VZAS2S3,
            VZAS2,
            VZAS3,
            ASS2S3,
            ASS2,
            ASS3,
            Localnet_Strom,
            Localnet_Gas,
            Localnet_Wärme,
            BFH_EnergieträgerHeizung,
            BFH_EnergieBedarfHeizung,
            BFH_Einwohner,
            BFH_Geschäfte,
            BFH_PVSystemGrösseInKWh,
            BFH_StromHaushalteLow,
            BFH_StromHaushalteHigh,
            BFH_StromGewerbeLow,
            BFH_StromGewerbeHigh,
            FeuerungsstättenArt,
            FeuerungsstättenKesselLeistung,
            FeuerungsstättenJahresEnergie1500hGas,
            FeuerungsstättenJahresEnergie2200hGas,
            FeuerungsstättenJahresEnergie1500hOel,
            FeuerungsstättenJahresEnergie2200hOel,
            BFH_AnzahlFahrzeuge,
            BFH_SummeAutoPendlerdistanz,
            BFH_SummeAutoGesamtdistanz,
            //BFH_GasGewerbe,
            BFH_SonnendachPotential,
            TrafoKreis
        }

        protected override void RunActualProcess()
        {
            //string excelFileName = @"U:\SimZukunft\RawDataForMerging\DatenfürEnergiebilanzBurgdorf2018.xlsx";
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbComplexes = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;

            var complexes = dbComplexes.Fetch<BuildingComplex>();
            var gwr = dbRaw.Fetch<GwrData>();
            var kanton = dbRaw.Fetch<EnergiebedarfsdatenBern>();
            var complexEnergy = dbComplexEnergy.Fetch<MonthlyElectricityUsePerStandort>();
            var houses = dbHouses.Fetch<House>();
            var heatingSystems = dbHouses.Fetch<HeatingSystemEntry>();
            var pvSystemEntries = dbHouses.Fetch<PvSystemEntry>();
            var occupants = dbHouses.Fetch<Occupant>();
            var businessEntries = dbHouses.Fetch<BusinessEntry>();
            var households = dbHouses.Fetch<Household>();
            var carDistances = dbHouses.Fetch<CarDistanceEntry>();
            using (var p = new ExcelPackage()) {
                //A workbook must have at least on cell, so lets add one...
                var ws = p.Workbook.Worksheets.Add("MySheet");
                //To set values in the spreadsheet use the Cells indexer.
                var columnNumbers = MakeColumnNumberDictionary();
                //header
                foreach (var pair in columnNumbers) {
                    ws.Cells[1, pair.Value].Value = pair.Key.ToString();
                }

                var row = 2;
                foreach (var house in houses) {
                    var complex = complexes.Single(x => x.ComplexName == house.ComplexName);
                    var heatingSystem = heatingSystems.Single(x => x.HouseGuid == house.HouseGuid);
                    var pvSystem = pvSystemEntries.FirstOrDefault(x => x.HouseGuid == house.HouseGuid);
                    var houseOccupants = occupants.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                    var houseBusinessEntries = businessEntries.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                    var houseHouseholds = households.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                    var houseCarDistances = carDistances.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                        WriteOneLine(ws, row, columnNumbers, house, gwr, kanton, complexEnergy, complex, heatingSystem, houseOccupants, houseBusinessEntries, pvSystem, houseHouseholds,
                            houseCarDistances);
                        row++;
                }

                //Save the new workbook. We haven't specified the filename so use the Save as method.
                var filename = MakeAndRegisterFullFilename("BrunoHariExport.xlsx", Name, "", Constants.PresentSlice);
                p.SaveAs(new FileInfo(filename));
                Log(MessageType.Warning, "File: " + filename);
            }
        }

        [NotNull]
        public string CleanJson([NotNull] string s) => s.Replace("[", "").Replace("]", "");

        // ReSharper disable once FunctionComplexityOverflow
        private void WriteOneLine([NotNull] ExcelWorksheet ws, int row, [NotNull] Dictionary<Columns, int> columnNumbers, [NotNull] House house, [ItemNotNull] [NotNull] List<GwrData> gwr,
                                  [ItemNotNull] [NotNull] List<EnergiebedarfsdatenBern> kanton, [ItemNotNull] [NotNull] List<MonthlyElectricityUsePerStandort> complexEnergy,
                                  [NotNull] BuildingComplex mycomplex, [NotNull] HeatingSystemEntry heatingSystem, [NotNull] [ItemNotNull] List<Occupant> occupants, [NotNull] [ItemNotNull] List<BusinessEntry> businessEntries,
                                  [CanBeNull] PvSystemEntry pvsystem, [NotNull] [ItemNotNull] List<Household> households, [NotNull] [ItemNotNull] List<CarDistanceEntry> carDistances)
        {
            ws.Cells[row, columnNumbers[Columns.ComplexName]].Value = house.ComplexName;
            ws.Cells[row, columnNumbers[Columns.Adressen]].Value = mycomplex.AdressesAsJson;
            ws.Cells[row, columnNumbers[Columns.EGids]].Value = CleanJson(mycomplex.EGIDsAsJson);
            ws.Cells[row, columnNumbers[Columns.LocalnetISNIds]].Value = CleanJson(mycomplex.GebäudeObjectIDsAsJson);
            ws.Cells[row, columnNumbers[Columns.GeoKoordinaten]].Value = CleanJson(mycomplex.GeoCoordsAsJson);
            var gwrs = gwr.Where(x => {
                Debug.Assert(x.EidgGebaeudeidentifikator_EGID != null, "x.EidgGebaeudeidentifikator_EGID != null");
                return mycomplex.EGids.Contains((long)x.EidgGebaeudeidentifikator_EGID);
            }).ToList();
            ws.Cells[row, columnNumbers[Columns.WG_GWR_GebäudeAnzahl]].Value = gwrs.Count;

            var ebbes = kanton.Where(x => mycomplex.EGids.Contains(x.egid)).ToList();
            ws.Cells[row, columnNumbers[Columns.WG_GEAK_Anzahl]].Value = ebbes.Sum(x => x.has_geak);
            ws.Cells[row, columnNumbers[Columns.EBBE_GebäudeTyp]].Value = CollapseList(ebbes.Select(x => x.upd_gtyp));
            ws.Cells[row, columnNumbers[Columns.EBBE_GebäudeTyp]].Value = ws.Cells[row, columnNumbers[Columns.GWR_WG]].Value = gwrs.Sum(x => x.AnzahlWohnungen_GANZWHG);
            ws.Cells[row, columnNumbers[Columns.EBBE_GArea]].Value = ebbes.Sum(x => x.garea);
            ws.Cells[row, columnNumbers[Columns.EBBE_EBF_HZ_Updated]].Value = ebbes.Sum(x => x.upd_ebf);
            ws.Cells[row, columnNumbers[Columns.EBBE_calc_ehzww]].Value = ebbes.Sum(x => x.calc_ehzww);
            ws.Cells[row, columnNumbers[Columns.EBBE_calc_ehz]].Value = ebbes.Sum(x => x.calc_ehz);
            ws.Cells[row, columnNumbers[Columns.EBBE_calc_eww]].Value = ebbes.Sum(x => x.calc_eww);

            //energieträger aus ebbe
            var heizträgerlst = ebbes.Select(x => x.upd_genhz).ToList();
            long heizträger = 0;
            if (heizträgerlst.Count > 0) {
                heizträger = heizträgerlst[0];
            }

            switch (heizträger) {
                case 0:
                    break;
                case 7200:
                    break;
                case 7201:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_OL]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7202:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_KO]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7203:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_GZ]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7204:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_EL]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7205:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_HO]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7206:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_WP]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7207:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_SO]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7208:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_FW]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7209:
                    ws.Cells[row, columnNumbers[Columns.EBHZ_A]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                default: throw new Exception("Unknown Heizungsträger: " + heizträger);
            }

            //warmwasser aus Ebbe
            var wwträgerlst = ebbes.Select(x => x.upd_genww).ToList();
            long wwträger = 0;
            if (wwträgerlst.Count > 0) {
                wwträger = wwträgerlst[0];
            }

            switch (wwträger) {
                case 0:
                    break;
                case 7200:
                    break;
                case 7201:
                    ws.Cells[row, columnNumbers[Columns.EBWW_OL]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7202:
                    ws.Cells[row, columnNumbers[Columns.EBWW_KO]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7203:
                    ws.Cells[row, columnNumbers[Columns.EBWW_GZ]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7204:
                    ws.Cells[row, columnNumbers[Columns.EBWW_EL]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7205:
                    ws.Cells[row, columnNumbers[Columns.EBWW_HO]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7206:
                    ws.Cells[row, columnNumbers[Columns.EBWW_WP]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7207:
                    ws.Cells[row, columnNumbers[Columns.EBWW_SO]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7208:
                    ws.Cells[row, columnNumbers[Columns.EBWW_FW]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7209:
                    ws.Cells[row, columnNumbers[Columns.EBWW_A]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                default: throw new Exception("Unknown Heizungsträger: " + heizträger);
            }

            /*

            
            ,
            EBWW_U,
            EBWW_OL,
            EBWW_KO,
            EBWW_GZ,
            EBWW_EL,
            EBWW_HO,
            EBWW_WP,
            EBWW_SO,
            EBWW_FW,
            EBWW_A,

    */
            ws.Cells[row, columnNumbers[Columns.EBBE_EnergieträgerHz]].Value = CollapseList(ebbes.Select(x => x.upd_genhz.ToString()));
            ws.Cells[row, columnNumbers[Columns.EBBE_EnergieträgerWW]].Value = CollapseList(ebbes.Select(x => x.upd_genww.ToString()));
            //entries from localnet
            var monthlies = complexEnergy.Where(x => mycomplex.CleanedStandorte.Contains(x.CleanedStandort)).ToList();
            ws.Cells[row, columnNumbers[Columns.Localnet_Strom]].Value = monthlies.Sum(x => x.YearlyElectricityUseNetz);
            ws.Cells[row, columnNumbers[Columns.Localnet_Gas]].Value = monthlies.Sum(x => x.YearlyGasUse);
            ws.Cells[row, columnNumbers[Columns.Localnet_Wärme]].Value = monthlies.Sum(x => x.YearlyFernwaermeUse);
            ws.Cells[row, columnNumbers[Columns.BFH_EnergieträgerHeizung]].Value = heatingSystem.SynthesizedHeatingSystemType.ToString();
            ws.Cells[row, columnNumbers[Columns.BFH_EnergieBedarfHeizung]].Value = heatingSystem.YearlyEnergyDemand;
            ws.Cells[row, columnNumbers[Columns.BFH_Einwohner]].Value = occupants.Count;
            var businessTypes = businessEntries.Select(x => x.BusinessType.ToString()).Distinct().ToList();
            var businessTypeStr = string.Join(",", businessTypes);
            ws.Cells[row, columnNumbers[Columns.BFH_Geschäfte]].Value = businessTypeStr;
            if (pvsystem != null) {
                ws.Cells[row, columnNumbers[Columns.BFH_PVSystemGrösseInKWh]].Value = pvsystem.YearlyPotential;
            }

            ws.Cells[row, columnNumbers[Columns.BFH_Geschäfte]].Value = businessTypeStr;
            ws.Cells[row, columnNumbers[Columns.BFH_StromHaushalteLow]].Value =
                households.Sum(x => x.LowVoltageYearlyTotalElectricityUse);
            ws.Cells[row, columnNumbers[Columns.BFH_StromHaushalteLow]].Value =
                households.Sum(x => x.HighVoltageYearlyTotalElectricityUse);
            ws.Cells[row, columnNumbers[Columns.BFH_StromGewerbeLow]].Value = businessEntries.Sum(x => x.LowVoltageYearlyTotalElectricityUse);
            ws.Cells[row, columnNumbers[Columns.BFH_StromGewerbeHigh]].Value = businessEntries.Sum(x => x.HighVoltageYearlyTotalElectricityUse);
            //ws.Cells[row, columnNumbers[Columns.BFH_GasGewerbe]].Value = businessEntries.Sum(x => x.YearlyGasUse);
            ws.Cells[row, columnNumbers[Columns.FeuerungsstättenArt]].Value = heatingSystem.FeuerungsstättenType;
            ws.Cells[row, columnNumbers[Columns.FeuerungsstättenKesselLeistung]].Value = heatingSystem.FeuerungsstättenPower;
            if (heatingSystem.FeuerungsstättenType == "Gas") {
                ws.Cells[row, columnNumbers[Columns.FeuerungsstättenJahresEnergie1500hGas]].Value = heatingSystem.EstimatedMinimumEnergyFromFeuerungsStätten;
                ws.Cells[row, columnNumbers[Columns.FeuerungsstättenJahresEnergie2200hGas]].Value = heatingSystem.EstimatedMaximumEnergyFromFeuerungsStätten;
            }
            if (heatingSystem.FeuerungsstättenType == "Oel")
            {
                ws.Cells[row, columnNumbers[Columns.FeuerungsstättenJahresEnergie1500hOel]].Value = heatingSystem.EstimatedMinimumEnergyFromFeuerungsStätten;
                ws.Cells[row, columnNumbers[Columns.FeuerungsstättenJahresEnergie2200hOel]].Value = heatingSystem.EstimatedMaximumEnergyFromFeuerungsStätten;
            }
            ws.Cells[row, columnNumbers[Columns.BFH_AnzahlFahrzeuge]].Value = carDistances.Count;
            ws.Cells[row, columnNumbers[Columns.BFH_SummeAutoPendlerdistanz]].Value = carDistances.Sum(x=> x.CommutingDistance)*365;
            ws.Cells[row, columnNumbers[Columns.BFH_SummeAutoGesamtdistanz]].Value = carDistances.Sum(x=> x.TotalDistance)*365;
            ws.Cells[row, columnNumbers[Columns.BFH_SonnendachPotential]].Value = heatingSystem.FeuerungsstättenPower;
            ws.Cells[row, columnNumbers[Columns.TrafoKreis]].Value = string.Join(";", house.Hausanschluss.Select(x=> x.Trafokreis));
        }


        [NotNull]
        private string CollapseList([ItemNotNull] [NotNull] IEnumerable<string> strs)
        {
            var s = "";
            var builder = new System.Text.StringBuilder();
            builder.Append(s);
            foreach (var str in strs) {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (str != null) {
                    builder.Append(str.Trim() + ", ");
                }
            }

            s = builder.ToString();
            if (s.EndsWith(", ")) {
                s = s.Substring(0, s.Length - 2);
            }

            return s;
        }

        [NotNull]
        private static Dictionary<Columns, int> MakeColumnNumberDictionary()
        {
            var columnNumbers = new Dictionary<Columns, int>();
            var column = 1;
            foreach (Columns value in Enum.GetValues(typeof(Columns))) {
                columnNumbers.Add(value, column++);
            }

            return columnNumbers;
        }
    }
}
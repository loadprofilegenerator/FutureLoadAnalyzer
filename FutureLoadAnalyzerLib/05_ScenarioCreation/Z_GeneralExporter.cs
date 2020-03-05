using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Dst;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using OfficeOpenXml;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    // ReSharper disable once InconsistentNaming
    public class Z_GeneralExporter : RunableForSingleSliceWithBenchmark {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum Column {
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
            TrafoKreis,
            BFH_HouseholdEnergyUse,
            BFH_Infrastructure,
            BFH_Photovoltaik,
            BFH_BusinessNoLastgangLowVoltage,
            BFH_Kwkw,
            BFH_BusinessWithLastgangLowVoltage,
            BFH_HouseLoad,
            BFH_HouseGeneration,
            BFH_LastgangGeneration,
            BFH_StreetLight,
            BFH_BusinessNoLastgangHighVoltage,
            BFH_BusinessWithLastgangHighVoltage,
            BFH_OutboundElectricCommuter,
            BFH_Heating,
            BFH_Cooling,
            BFH_Dhw
        }

        public Z_GeneralExporter([NotNull] ServiceRepository services)
            : base(nameof(Z_GeneralExporter), Stage.ScenarioCreation, 2600, services, false) {
            DevelopmentStatus.Add("Gas Gewerbe richtig reinmachen");
        }

        [NotNull]
        public static string CleanJson([NotNull] string s) => s.Replace("[", "").Replace("]", "");

        protected override void RunActualProcess(ScenarioSliceParameters slice)
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbComplexes = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var dbComplexEnergy = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice);
            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);

            var complexes = dbComplexes.Fetch<BuildingComplex>();
            var gwr = dbRaw.Fetch<GwrData>();
            var kanton = dbRaw.Fetch<EnergiebedarfsdatenBern>();
            var complexEnergy = dbComplexEnergy.Fetch<MonthlyElectricityUsePerStandort>();
            var houses = dbHouses.Fetch<House>();
            var heatingSystems = dbHouses.Fetch<HeatingSystemEntry>();
            var pvSystemEntries = dbHouses.Fetch<PvSystemEntry>();
            var businessEntries = dbHouses.Fetch<BusinessEntry>();
            var households = dbHouses.Fetch<Household>();
            var carDistances = dbHouses.Fetch<CarDistanceEntry>();
            HouseComponentRepository hcr = new HouseComponentRepository(dbHouses);
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
                    var heatingSystem = heatingSystems.Single(x => x.HouseGuid == house.Guid);
                    var pvSystem = pvSystemEntries.FirstOrDefault(x => x.HouseGuid == house.Guid);
                    var houseHouseholds = households.Where(x => x.HouseGuid == house.Guid).ToList();
                    var houseOccupants = houseHouseholds.SelectMany(x=> x.Occupants).ToList();
                    var houseBusinessEntries = businessEntries.Where(x => x.HouseGuid == house.Guid).ToList();
                    var houseCarDistances = carDistances.Where(x => x.HouseGuid == house.Guid).ToList();
                    WriteOneLine(ws,
                        row,
                        columnNumbers,
                        house,
                        gwr,
                        kanton,
                        complexEnergy,
                        complex,
                        heatingSystem,
                        houseOccupants,
                        houseBusinessEntries,
                        pvSystem,
                        houseHouseholds,
                        houseCarDistances,
                        hcr);
                    row++;
                }

                //Save the new workbook. We haven't specified the filename so use the Save as method.
                var filename = MakeAndRegisterFullFilename("BrunoHariExport." + slice + ".xlsx", slice);
                p.SaveAs(new FileInfo(filename));
                Warning("File: " + filename);
                SaveToArchiveDirectory(filename,RelativeDirectory.Bruno,slice);
            }
        }


        [NotNull]
        private static string CollapseList([ItemNotNull] [NotNull] IEnumerable<string> strs)
        {
            var s = "";
            var builder = new StringBuilder();
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
        private static Dictionary<Column, int> MakeColumnNumberDictionary()
        {
            var columnNumbers = new Dictionary<Column, int>();
            var column = 1;
            foreach (Column value in Enum.GetValues(typeof(Column))) {
                columnNumbers.Add(value, column++);
            }

            return columnNumbers;
        }

        // ReSharper disable once FunctionComplexityOverflow
        private static void WriteOneLine([NotNull] ExcelWorksheet ws,
                                         int row,
                                         [NotNull] Dictionary<Column, int> columnNumbers,
                                         [NotNull] House house,
                                         [ItemNotNull] [NotNull] List<GwrData> gwr,
                                         [ItemNotNull] [NotNull] List<EnergiebedarfsdatenBern> kanton,
                                         [ItemNotNull] [NotNull] List<MonthlyElectricityUsePerStandort> complexEnergy,
                                         [NotNull] BuildingComplex mycomplex,
                                         [NotNull] HeatingSystemEntry heatingSystem,
                                         [NotNull] [ItemNotNull] List<Occupant> occupants,
                                         [NotNull] [ItemNotNull] List<BusinessEntry> businessEntries,
                                         [CanBeNull] PvSystemEntry pvsystem,
                                         [NotNull] [ItemNotNull] List<Household> households,
                                         [NotNull] [ItemNotNull] List<CarDistanceEntry> carDistances,
                                         [NotNull] HouseComponentRepository hcr)
        {
            ws.Cells[row, columnNumbers[Column.ComplexName]].Value = house.ComplexName;
            ws.Cells[row, columnNumbers[Column.Adressen]].Value = mycomplex.AdressesAsJson;
            ws.Cells[row, columnNumbers[Column.EGids]].Value = CleanJson(mycomplex.EGIDsAsJson);
            ws.Cells[row, columnNumbers[Column.LocalnetISNIds]].Value = CleanJson(mycomplex.GebäudeObjectIDsAsJson);
            ws.Cells[row, columnNumbers[Column.GeoKoordinaten]].Value = CleanJson(mycomplex.GeoCoordsAsJson);
            var gwrs = gwr.Where(x => {
                if (x.EidgGebaeudeidentifikator_EGID == null) {
                    throw new FlaException("x.EidgGebaeudeidentifikator_EGID != null");
                }

                return mycomplex.EGids.Contains((long)x.EidgGebaeudeidentifikator_EGID);
            }).ToList();
            ws.Cells[row, columnNumbers[Column.WG_GWR_GebäudeAnzahl]].Value = gwrs.Count;

            var ebbes = kanton.Where(x => mycomplex.EGids.Contains(x.egid)).ToList();
            ws.Cells[row, columnNumbers[Column.WG_GEAK_Anzahl]].Value = ebbes.Sum(x => x.has_geak);
            ws.Cells[row, columnNumbers[Column.EBBE_GebäudeTyp]].Value = CollapseList(ebbes.Select(x => x.upd_gtyp));
            ws.Cells[row, columnNumbers[Column.EBBE_GebäudeTyp]].Value =
                ws.Cells[row, columnNumbers[Column.GWR_WG]].Value = gwrs.Sum(x => x.AnzahlWohnungen_GANZWHG);
            ws.Cells[row, columnNumbers[Column.EBBE_GArea]].Value = ebbes.Sum(x => x.garea);
            ws.Cells[row, columnNumbers[Column.EBBE_EBF_HZ_Updated]].Value = ebbes.Sum(x => x.upd_ebf);
            ws.Cells[row, columnNumbers[Column.EBBE_calc_ehzww]].Value = ebbes.Sum(x => x.calc_ehzww);
            ws.Cells[row, columnNumbers[Column.EBBE_calc_ehz]].Value = ebbes.Sum(x => x.calc_ehz);
            ws.Cells[row, columnNumbers[Column.EBBE_calc_eww]].Value = ebbes.Sum(x => x.calc_eww);

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
                    ws.Cells[row, columnNumbers[Column.EBHZ_OL]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7202:
                    ws.Cells[row, columnNumbers[Column.EBHZ_KO]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7203:
                    ws.Cells[row, columnNumbers[Column.EBHZ_GZ]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7204:
                    ws.Cells[row, columnNumbers[Column.EBHZ_EL]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7205:
                    ws.Cells[row, columnNumbers[Column.EBHZ_HO]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7206:
                    ws.Cells[row, columnNumbers[Column.EBHZ_WP]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7207:
                    ws.Cells[row, columnNumbers[Column.EBHZ_SO]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7208:
                    ws.Cells[row, columnNumbers[Column.EBHZ_FW]].Value = ebbes.Sum(x => x.calc_ehz);
                    break;
                case 7209:
                    ws.Cells[row, columnNumbers[Column.EBHZ_A]].Value = ebbes.Sum(x => x.calc_ehz);
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
                    ws.Cells[row, columnNumbers[Column.EBWW_OL]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7202:
                    ws.Cells[row, columnNumbers[Column.EBWW_KO]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7203:
                    ws.Cells[row, columnNumbers[Column.EBWW_GZ]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7204:
                    ws.Cells[row, columnNumbers[Column.EBWW_EL]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7205:
                    ws.Cells[row, columnNumbers[Column.EBWW_HO]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7206:
                    ws.Cells[row, columnNumbers[Column.EBWW_WP]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7207:
                    ws.Cells[row, columnNumbers[Column.EBWW_SO]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7208:
                    ws.Cells[row, columnNumbers[Column.EBWW_FW]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                case 7209:
                    ws.Cells[row, columnNumbers[Column.EBWW_A]].Value = ebbes.Sum(x => x.calc_eww);
                    break;
                default: throw new Exception("Unknown Heizungsträger: " + heizträger);
            }

            ws.Cells[row, columnNumbers[Column.EBBE_EnergieträgerHz]].Value = CollapseList(ebbes.Select(x => x.upd_genhz.ToString()));
            ws.Cells[row, columnNumbers[Column.EBBE_EnergieträgerWW]].Value = CollapseList(ebbes.Select(x => x.upd_genww.ToString()));
            //entries from localnet
            var monthlies = complexEnergy.Where(x => mycomplex.CleanedStandorte.Contains(x.CleanedStandort)).ToList();
            ws.Cells[row, columnNumbers[Column.Localnet_Strom]].Value = monthlies.Sum(x => x.YearlyElectricityUseNetz);
            ws.Cells[row, columnNumbers[Column.Localnet_Gas]].Value = monthlies.Sum(x => x.YearlyGasUse);
            ws.Cells[row, columnNumbers[Column.Localnet_Wärme]].Value = monthlies.Sum(x => x.YearlyFernwaermeUse);
            ws.Cells[row, columnNumbers[Column.BFH_EnergieträgerHeizung]].Value = heatingSystem.SynthesizedHeatingSystemType.ToString();
            ws.Cells[row, columnNumbers[Column.BFH_EnergieBedarfHeizung]].Value = heatingSystem.EffectiveEnergyDemand;
            ws.Cells[row, columnNumbers[Column.BFH_Einwohner]].Value = occupants.Count;
            var businessTypes = businessEntries.Select(x => x.BusinessType.ToString()).Distinct().ToList();
            var businessTypeStr = string.Join(",", businessTypes);
            ws.Cells[row, columnNumbers[Column.BFH_Geschäfte]].Value = businessTypeStr;
            if (pvsystem != null) {
                ws.Cells[row, columnNumbers[Column.BFH_PVSystemGrösseInKWh]].Value = pvsystem.EffectiveEnergyDemand;
            }

            ws.Cells[row, columnNumbers[Column.BFH_Geschäfte]].Value = businessTypeStr;
            ws.Cells[row, columnNumbers[Column.BFH_StromHaushalteLow]].Value = households.Sum(x => x.LocalnetLowVoltageYearlyTotalElectricityUse);
            ws.Cells[row, columnNumbers[Column.BFH_StromHaushalteLow]].Value = households.Sum(x => x.LocalnetHighVoltageYearlyTotalElectricityUse);
            ws.Cells[row, columnNumbers[Column.BFH_StromGewerbeLow]].Value = businessEntries.Sum(x => x.LocalnetLowVoltageYearlyTotalElectricityUse);
            ws.Cells[row, columnNumbers[Column.BFH_StromGewerbeHigh]].Value =
                businessEntries.Sum(x => x.LocalnetHighVoltageYearlyTotalElectricityUse);
            ws.Cells[row, columnNumbers[Column.FeuerungsstättenArt]].Value = heatingSystem.FeuerungsstättenType;
            ws.Cells[row, columnNumbers[Column.FeuerungsstättenKesselLeistung]].Value = heatingSystem.FeuerungsstättenPower;
            if (heatingSystem.FeuerungsstättenType == "Gas") {
                ws.Cells[row, columnNumbers[Column.FeuerungsstättenJahresEnergie1500hGas]].Value =
                    heatingSystem.EstimatedMinimumEnergyFromFeuerungsStätten;
                ws.Cells[row, columnNumbers[Column.FeuerungsstättenJahresEnergie2200hGas]].Value =
                    heatingSystem.EstimatedMaximumEnergyFromFeuerungsStätten;
            }

            if (heatingSystem.FeuerungsstättenType == "Oel") {
                ws.Cells[row, columnNumbers[Column.FeuerungsstättenJahresEnergie1500hOel]].Value =
                    heatingSystem.EstimatedMinimumEnergyFromFeuerungsStätten;
                ws.Cells[row, columnNumbers[Column.FeuerungsstättenJahresEnergie2200hOel]].Value =
                    heatingSystem.EstimatedMaximumEnergyFromFeuerungsStätten;
            }

            ws.Cells[row, columnNumbers[Column.BFH_AnzahlFahrzeuge]].Value = carDistances.Count;
            ws.Cells[row, columnNumbers[Column.BFH_SummeAutoPendlerdistanz]].Value = carDistances.Sum(x => x.CommutingDistance) * 365;
            ws.Cells[row, columnNumbers[Column.BFH_SummeAutoGesamtdistanz]].Value = carDistances.Sum(x => x.TotalDistance) * 365;
            ws.Cells[row, columnNumbers[Column.BFH_SonnendachPotential]].Value = heatingSystem.FeuerungsstättenPower;
            ws.Cells[row, columnNumbers[Column.TrafoKreis]].Value = string.Join(";", house.Hausanschluss.Select(x => x.Trafokreis));
            var houseComponents = house.CollectHouseComponents(hcr);
            Dictionary<HouseComponentType, double> houseComponentSums = new Dictionary<HouseComponentType, double>();
            foreach (var hc in houseComponents) {
                if (!houseComponentSums.ContainsKey(hc.HouseComponentType)) {
                    houseComponentSums.Add(hc.HouseComponentType, 0);
                }

                houseComponentSums[hc.HouseComponentType] += hc.EffectiveEnergyDemand;
            }

            foreach (var entry in houseComponentSums) {
                Column mycol = GetColumn(entry.Key);
                ws.Cells[row, columnNumbers[mycol]].Value = entry.Value;
            }
        }

        private static Column GetColumn(HouseComponentType hct)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (hct) {
                case HouseComponentType.Household:
                    return Column.BFH_HouseholdEnergyUse;
                case HouseComponentType.Infrastructure:
                    return Column.BFH_Infrastructure;
                case HouseComponentType.Photovoltaik:
                    return Column.BFH_Photovoltaik;
                case HouseComponentType.BusinessNoLastgangLowVoltage:
                    return Column.BFH_BusinessNoLastgangLowVoltage;
                case HouseComponentType.Kwkw:
                    return Column.BFH_Kwkw;
                case HouseComponentType.BusinessWithLastgangLowVoltage:
                    return Column.BFH_BusinessWithLastgangLowVoltage;
                case HouseComponentType.HouseLoad:
                    return Column.BFH_HouseLoad;
                case HouseComponentType.HouseGeneration:
                    return Column.BFH_HouseGeneration;
                case HouseComponentType.LastgangGeneration:
                    return Column.BFH_LastgangGeneration;
                case HouseComponentType.StreetLight:
                    return Column.BFH_StreetLight;
                case HouseComponentType.BusinessNoLastgangHighVoltage:
                    return Column.BFH_BusinessNoLastgangHighVoltage;
                case HouseComponentType.BusinessWithLastgangHighVoltage:
                    return Column.BFH_BusinessWithLastgangHighVoltage;
                case HouseComponentType.OutboundElectricCommuter:
                    return Column.BFH_OutboundElectricCommuter;
                case HouseComponentType.Heating:
                    return Column.BFH_Heating;
                case HouseComponentType.Cooling:
                    return Column.BFH_Cooling;
                case HouseComponentType.Dhw:
                    return Column.BFH_Dhw;
                default: throw new FlaException("No match");
            }
        }
    }
}
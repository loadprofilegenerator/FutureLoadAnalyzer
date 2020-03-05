using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.ResultFiles;
using Common.Steps;
using Data;
using Data.DataModel.Dst;
using JetBrains.Annotations;
using Visualizer.Sankey;

namespace BurgdorfStatistics._03_KomplexEnergy {
    // ReSharper disable once InconsistentNaming
    internal class E_MakeValidationCharts : RunableWithBenchmark {
        public E_MakeValidationCharts([NotNull] ServiceRepository services)
            : base(nameof(E_MakeValidationCharts), Stage.ComplexEnergyData, 15, services, true)
        {
        }

        protected override void RunActualProcess()
        {
        }

        protected override void RunChartMaking()
        {
            var dbComplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var dbComplexEnergie = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;
            var complexes = dbComplex.Fetch<BuildingComplex>();


            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var ebbe = dbRaw.Fetch<EnergiebedarfsdatenBern>();
            //var complexes = dbComplex.Fetch<BuildingComplex>();
            var complexBuildingData = dbComplexEnergie.Fetch<ComplexBuildingData>();
            MakeComplexAreaSankey(Constants.PresentSlice);

            //var sonnendach = dbRaw.Fetch<SonnenDach>();
            MakeBuildingSizeComplex(Constants.PresentSlice);
            SizeHistogram();
            MovedCharts();
            void MakeBuildingSizeComplex(ScenarioSliceParameters slice)
            {
                var ssa = new SingleSankeyArrow("BuildingSizeComplex", 1000, MyStage, SequenceNumber,
                    Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Complexes", complexes.Count, 2000, Orientation.Straight));
                var complexWithOneBuilding = complexes.Count(x => x.EGids.Count == 1);
                var complexWithTwoBuilding = complexes.Count(x => x.EGids.Count == 2);
                var complexWithThreeBuilding = complexes.Count(x => x.EGids.Count == 3);
                var complexWithManyBuilding = complexes.Count(x => x.EGids.Count > 3);
                ssa.AddEntry(new SankeyEntry("Komplexe mit 1 Gebäude", complexWithOneBuilding * -1, 1000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Komplexe mit 2 Gebäuden", complexWithTwoBuilding * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Komplexe mit 3 Gebäuden", complexWithThreeBuilding * -1, 3000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Komplexe mit mehr als 3 Gebäuden", complexWithManyBuilding * -1, 3000, Orientation.Up));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeComplexAreaSankey(ScenarioSliceParameters slice)

            {
                var ssa = new SingleSankeyArrow("ComplexSankeyTotalArea", 1000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var ebbeEnergieBezugsfläche = ebbe.Select(x => x.upd_ebf).Sum() / 1000;
                var complexBuildingDataArea = complexBuildingData.Select(x => x.TotalEnergieBezugsfläche).Sum() / 1000;
                var diff = ebbeEnergieBezugsfläche - complexBuildingDataArea;
                ssa.AddEntry(new SankeyEntry("Ebbe EBF", ebbeEnergieBezugsfläche, 5000, Orientation.Straight));

                ssa.AddEntry(new SankeyEntry("Complex EBF ", complexBuildingDataArea * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Fehlend", diff * -1, 5000, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void SizeHistogram()
            {
                var counts = new Dictionary<int, int>();
                foreach (var complex in complexes) {
                    if (!counts.ContainsKey(complex.EGids.Count)) {
                        counts.Add(complex.EGids.Count, 0);
                    }

                    counts[complex.EGids.Count]++;
                }

                var filename = MakeAndRegisterFullFilename("EgidPerComplexHistogram.png", Name, "", Constants.PresentSlice);
                var names = new List<string>();
                var barSeries = new List<BarSeriesEntry>();
                var column = 0;
                foreach (var pair in counts) {
                    names.Add(pair.Value.ToString());
                    var count = pair.Value;
                    barSeries.Add(BarSeriesEntry.MakeBarSeriesEntry(pair.Key.ToString(), count, column));
                    column++;
                }

                Services.PlotMaker.MakeBarChart(filename, "", barSeries, names);

            }


            //read database
                var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;
                var monthlyElectricityUsePerStandorts = dbComplexEnergy.Fetch<MonthlyElectricityUsePerStandort>();
                var localnetData = dbRaw.Fetch<Localnet>();
                var coords = 0;
                foreach (var complex in complexes)
                {
                    coords += complex.Coords.Count;
                }

                Log(MessageType.Info, "UsedCoords = " + coords);
                var arrows = new List<SingleSankeyArrow>();
                arrows.AddRange(MakeElectricityArrow(complexes, localnetData, monthlyElectricityUsePerStandorts,MyStage));
                arrows.AddRange(MakeGasArrow(complexes, localnetData, monthlyElectricityUsePerStandorts));
                arrows.AddRange(MakeFernwärmeArrow(complexes, localnetData, monthlyElectricityUsePerStandorts));
                Directory.SetCurrentDirectory(Constants.BasePath);
                foreach (var arrow in arrows)
                {
                    Services.PlotMaker.MakeSankeyChart(arrow);
                }
            }

        private void MovedCharts()
        {
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var localNet = dbRaw.Fetch<Localnet>();
            var sumPerVerrechnungstyp = new Dictionary<string, double>();
            var verrechnungstypen = localNet.Select(x => x.Verrechnungstyp).Distinct().ToList();
            var sumPerTarif = new Dictionary<string, double>();
            var tarife = localNet.Select(x => x.Tarif).Distinct().ToList();
            foreach (var s in verrechnungstypen)
            {
                sumPerVerrechnungstyp.Add(s, 0);
            }

            foreach (var s in tarife)
            {
                sumPerTarif.Add(s, 0);
            }

            foreach (var l in localNet)
            {
                //ignore all the entries that don't have a standort
                if (string.IsNullOrWhiteSpace(l.Objektstandort))
                {
                    continue;
                }

                if (l.BasisVerbrauch != null && l.Verrechnungstyp != null)
                {
                    sumPerVerrechnungstyp[l.Verrechnungstyp] += l.BasisVerbrauch.Value;
                }

                if (l.BasisVerbrauch != null && l.Tarif != null)
                {
                    sumPerTarif[l.Tarif] += l.BasisVerbrauch.Value;
                }
            }

            foreach (var pair in sumPerVerrechnungstyp)
            {
                Log(MessageType.Info, "Verrechnungstyp:" + pair.Key + ": " + pair.Value);
            }

            foreach (var pair in sumPerTarif)
            {
                Log(MessageType.Info, "Tarif:" + pair.Key + ": " + pair.Value);
            }

            //electricity
            var targetSumElectricity = sumPerVerrechnungstyp["Netz Tagesstrom (HT)"] + sumPerVerrechnungstyp["Netz Nachtstrom (NT)"];
            var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice).Database;
            var complexEnergy = dbComplexEnergy.Fetch<MonthlyElectricityUsePerStandort>();
            var yearlyElectricityUseNetz = complexEnergy.Select(x => x.YearlyElectricityUseNetz).Sum();
            Log(MessageType.Info, "Yearly Electricty netz complexes:" + yearlyElectricityUseNetz);
            if (Math.Abs(targetSumElectricity - yearlyElectricityUseNetz) > 0.0001)
            {
                throw new Exception("Yearly sums dont fit for electricity: Target: " + targetSumElectricity / 1_000_000 + " complex:" + yearlyElectricityUseNetz / 1_000_000);
            }

            //gas
            var gasuseTarget = sumPerVerrechnungstyp["Erdgasverbrauch"];
            var gasuseComplexes = complexEnergy.Select(x => x.YearlyGasUse).Sum();

            Log(MessageType.Info, "Yearly gas complexes:" + gasuseComplexes);

            if (Math.Abs(gasuseTarget - gasuseComplexes) > 0.1)
            {
                throw new Exception("Yearly sums dont fit for electricity: Target: " + gasuseTarget + " complex:" + gasuseComplexes);
            }
    }



        [ItemNotNullAttribute]
        [NotNullAttribute]
        // ReSharper disable once FunctionComplexityOverflow
        private List<SingleSankeyArrow> MakeElectricityArrow([ItemNotNull] [NotNull] List<BuildingComplex> complexes, [ItemNotNull] [NotNull] List<Localnet> rawEntries,
                                 [ItemNotNull] [NotNull] List<MonthlyElectricityUsePerStandort> monthlyElectricityUsePerStandorts,
                                                             Stage mystage)
        {
            const string section = "Stromverbräuche";
            const string sectionDescription = "Analyse der Stromverbräuche";
            var slice = Constants.PresentSlice;
            //write python
            var arrows = new List<SingleSankeyArrow>();
            {
                var arr1 = new SingleSankeyArrow("Stromkunden_Anzahl_nach_Verrechnungstyp", 20000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, mystage);
                rfe.Save();
                var verbrauchsList = rawEntries.Where(x => x.Verrechnungstyp == "Netz Nachtstrom (NT)" || x.Verrechnungstyp == "Netz Tagesstrom (HT)").ToList();
                arr1.AddEntry(new SankeyEntry("Total", verbrauchsList.Count, 100, Orientation.Up));
                var count2 = rawEntries.Count(x => x.Verrechnungstyp == "Netz Nachtstrom (NT)");
                arr1.AddEntry(new SankeyEntry("Nachtstrom", count2 * -1, 100, Orientation.Up));
                var countTag = rawEntries.Count(x => x.Verrechnungstyp == "Netz Tagesstrom (HT)");
                arr1.AddEntry(new SankeyEntry("Tagstrom", countTag * -1, 100, Orientation.Up));
                arrows.Add(arr1);
            }
            {
                var arr1 = new SingleSankeyArrow("Stromkunden_Verbrauch_nach_Verrechnungstyp", 2000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, mystage);
                rfe.Save();
                var verbrauchsList = rawEntries.Where(x => x.Verrechnungstyp == "Netz Nachtstrom (NT)" || x.Verrechnungstyp == "Netz Tagesstrom (HT)").ToList();

                var totalSum = verbrauchsList.Select(x => {
                    if (x.BasisVerbrauch == null)
                    {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Total", totalSum / factor, 1000, Orientation.Up));
                var sumNight = rawEntries.Where(x => x.Verrechnungstyp == "Netz Nachtstrom (NT)").Select(x => {
                    if (x.BasisVerbrauch == null)
                    {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                arr1.AddEntry(new SankeyEntry("Nachtstrom", sumNight / factor * -1, 1000, Orientation.Down));
                var sumDay = rawEntries.Where(x => x.Verrechnungstyp == "Netz Tagesstrom (HT)").Select(x => {
                    if (x.BasisVerbrauch == null)
                    {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                arr1.AddEntry(new SankeyEntry("Tagstrom", sumDay / factor * -1, 1000, Orientation.Up));
                arrows.Add(arr1);
            }

            {
                var arr1 = new SingleSankeyArrow("Stromkunden_Verbrauch_Nach_Standort", 2000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "",
                    Constants.PresentSlice, MyStage);
                rfe.Save();
                var verbrauchsList = rawEntries.Where(x => x.Verrechnungstyp == "Netz Nachtstrom (NT)" || x.Verrechnungstyp == "Netz Tagesstrom (HT)").ToList();
                var totalSum = verbrauchsList.Select(x => {
                    if (x.BasisVerbrauch == null)
                    {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Total", totalSum / factor, 1000, Orientation.Up));
                var monthlyElectricityUseNetz = monthlyElectricityUsePerStandorts.Sum(x => x.YearlyElectricityUseNetz);
                arr1.AddEntry(new SankeyEntry("MonthlyElectricityUseNetz", monthlyElectricityUseNetz / factor * -1, 1000, Orientation.Down));
                arrows.Add(arr1);
            }

            {
                var arr1 = new SingleSankeyArrow("Stromkunden_Verbrauch_Nach_Rechnungsart", 2000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var verbrauchsList = rawEntries.Where(x => x.Verrechnungstyp == "Netz Nachtstrom (NT)" || x.Verrechnungstyp == "Netz Tagesstrom (HT)").ToList();
                var totalSum = verbrauchsList.Select(x => {
                    if (x.BasisVerbrauch == null)
                    {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Total", totalSum / factor, 1000, Orientation.Up));
                var rechungsarten = verbrauchsList.Select(x => x.Rechnungsart).Distinct().ToList();
                var pathlength = 500;
                foreach (var rechnungsart in rechungsarten)
                {
                    var rechnungsartSum = verbrauchsList.Where(x => x.Rechnungsart == rechnungsart).Select(x => {
                        if (x.BasisVerbrauch == null)
                        {
                            throw new Exception("Basisverbrauch was null");
                        }

                        return x.BasisVerbrauch.Value;
                    }).Sum();
                    arr1.AddEntry(new SankeyEntry(rechnungsart, rechnungsartSum / factor * -1, pathlength, Orientation.Down));
                    pathlength += 500;
                }

                arrows.Add(arr1);
            }

            {
                var arr1 = new SingleSankeyArrow("Stromkunden_Verbrauch_Nach_Identified_Complexed_Standort", 2000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var totalSum = monthlyElectricityUsePerStandorts.Select(x => x.YearlyElectricityUseNetz).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Total", totalSum / factor, 1000, Orientation.Up));
                var cleandStandortIDs = complexes.SelectMany(x => x.CleanedStandorte).ToList();
                var identifiedComplexStandorts = monthlyElectricityUsePerStandorts.Where(x => cleandStandortIDs.Contains(x.CleanedStandort));

                var identifiedmonthlyElectricityUseNetz = identifiedComplexStandorts.Sum(x => x.YearlyElectricityUseNetz);
                arr1.AddEntry(new SankeyEntry("MonthlyElectricityUseNetz", identifiedmonthlyElectricityUseNetz / factor * -1, 1000, Orientation.Down));
                arrows.Add(arr1);
            }

            {
                var arr1 = new SingleSankeyArrow("Stromkunden_Verbrauch_Nach_BuildingWithGeoCoords", 500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var totalSum = monthlyElectricityUsePerStandorts.Select(x => x.YearlyElectricityUseNetz).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Stromverbrauch gesamt", totalSum / factor, 500, Orientation.Straight));
                var cleandStandortIDs = complexes.Where(x => x.Coords.Count > 0).SelectMany(x => x.CleanedStandorte).ToList();
                var identifiedComplexStandorts = monthlyElectricityUsePerStandorts.Where(x => cleandStandortIDs.Contains(x.CleanedStandort));
                var identifiedmonthlyElectricityUseNetz = identifiedComplexStandorts.Sum(x => x.YearlyElectricityUseNetz);
                arr1.AddEntry(new SankeyEntry("Stromverbrauch in\\n Gebäuden mit Geokoordinaten", identifiedmonthlyElectricityUseNetz / factor * -1, 200, Orientation.Straight));

                var notidentified = monthlyElectricityUsePerStandorts.Where(x => !cleandStandortIDs.Contains(x.CleanedStandort));
                var notidentifiedmonthlyElectricityUseNetz = notidentified.Sum(x => x.YearlyElectricityUseNetz);
                arr1.AddEntry(new SankeyEntry("Stromverbrauch in nicht\\n zuordenbaren Gebäuden", notidentifiedmonthlyElectricityUseNetz / factor * -1, 200, Orientation.Up));

                arrows.Add(arr1);
            }
            return arrows;
        }

        [ItemNotNullAttribute]
        [NotNullAttribute]
        private List<SingleSankeyArrow> MakeGasArrow([ItemNotNull] [NotNull] List<BuildingComplex> complexes, [ItemNotNull] [NotNull] List<Localnet> rawEntries,
                                                     [ItemNotNull] [NotNull] List<MonthlyElectricityUsePerStandort> monthlyElectricityUsePerStandorts)
        {
            var slice = Constants.PresentSlice;
            const string section = "Erdgas";
            const string sectionDescription = "Analyse der Erdgasverbräuche";
            //write python
            var arrows = new List<SingleSankeyArrow>();
            {
                var arr1 = new SingleSankeyArrow("Erdgasverbrauch_NachStandorten", 2000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var verbrauchsList = rawEntries.Where(x => x.Verrechnungstyp == "Erdgasverbrauch").ToList();
                var totalSum = verbrauchsList.Select(x => {
                    if (x.BasisVerbrauch == null)
                    {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Total aus Rohdaten", totalSum / factor, 1000, Orientation.Up));
                var monthlyElectricityUseNetz = monthlyElectricityUsePerStandorts.Sum(x => x.YearlyGasUse);
                arr1.AddEntry(new SankeyEntry("Erdgasverbrauch in Monthly", monthlyElectricityUseNetz / factor * -1, 1000, Orientation.Down));
                arrows.Add(arr1);
            }

            {
                var arr1 = new SingleSankeyArrow("Erdgasverbrauchn_Verbrauch_Nach_Identified_Complexed_Standort", 2000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var totalSum = monthlyElectricityUsePerStandorts.Select(x => x.YearlyGasUse).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Total", totalSum / factor, 1000, Orientation.Up));
                var cleandStandortIDs = complexes.SelectMany(x => x.CleanedStandorte).ToList();
                var identifiedComplexStandorts = monthlyElectricityUsePerStandorts.Where(x => cleandStandortIDs.Contains(x.CleanedStandort));

                var identifiedmonthlygas = identifiedComplexStandorts.Sum(x => x.YearlyGasUse);
                arr1.AddEntry(new SankeyEntry("MonthlyElectricityUseNetz", identifiedmonthlygas / factor * -1, 1000, Orientation.Down));
                arrows.Add(arr1);
            }

            {
                var arr1 = new SingleSankeyArrow("Erdgasverbrauch_Verbrauch_Nach_BuildingWithGeoCoords", 500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var totalSum = monthlyElectricityUsePerStandorts.Select(x => x.YearlyGasUse).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Gasverbrauch gesamt", totalSum / factor, 500, Orientation.Straight));
                var cleandStandortIDs = complexes.Where(x => x.Coords.Count > 0).SelectMany(x => x.CleanedStandorte).ToList();
                var identifiedComplexStandorts = monthlyElectricityUsePerStandorts.Where(x => cleandStandortIDs.Contains(x.CleanedStandort));
                var identifiedmonthlyElectricityUseNetz = identifiedComplexStandorts.Sum(x => x.YearlyGasUse);
                arr1.AddEntry(new SankeyEntry("Gasverbrauch in\\n Gebäuden mit Geokoordinaten", identifiedmonthlyElectricityUseNetz / factor * -1, 200, Orientation.Straight));

                var notidentified = monthlyElectricityUsePerStandorts.Where(x => !cleandStandortIDs.Contains(x.CleanedStandort));
                var notidentifiedmonthlyElectricityUseNetz = notidentified.Sum(x => x.YearlyGasUse);
                arr1.AddEntry(new SankeyEntry("Gasverbrauch in nicht\\n zuordenbaren Gebäuden", notidentifiedmonthlyElectricityUseNetz / factor * -1, 200, Orientation.Up));

                arrows.Add(arr1);
            }
            return arrows;
        }


        [ItemNotNullAttribute]
        [NotNullAttribute]
        private List<SingleSankeyArrow> MakeFernwärmeArrow([ItemNotNull] [NotNull] List<BuildingComplex> complexes, [ItemNotNull] [NotNull] List<Localnet> rawEntries,
                                                           [ItemNotNull] [NotNull] List<MonthlyElectricityUsePerStandort> monthlyElectricityUsePerStandorts)
        {
            var slice = Constants.PresentSlice;
            const string section = "Erdgas";
            const string sectionDescription = "Analyse der Erdgasverbräuche";
            //write python
            var arrows = new List<SingleSankeyArrow>();
            {
                var arr1 = new SingleSankeyArrow("Fernwärme_NachStandorten", 2000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var verbrauchsList = rawEntries.Where(x => x.Verrechnungstyp == "Arbeitspreis").ToList();
                var totalSum = verbrauchsList.Select(x => {
                    if (x.BasisVerbrauch == null)
                    {
                        throw new Exception("Basisverbrauch was null");
                    }

                    return x.BasisVerbrauch.Value;
                }).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Total aus Rohdaten", totalSum / factor, 1000, Orientation.Up));
                var monthlyElectricityUseNetz = monthlyElectricityUsePerStandorts.Sum(x => x.YearlyFernwaermeUse);
                arr1.AddEntry(new SankeyEntry("Arbeitspreis in Monthly", monthlyElectricityUseNetz / factor * -1, 1000, Orientation.Down));
                arrows.Add(arr1);
            }

            {
                var arr1 = new SingleSankeyArrow("Fernwärme_Verbrauch_Nach_Identified_Complexed_Standort", 2000, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var totalSum = monthlyElectricityUsePerStandorts.Select(x => x.YearlyFernwaermeUse).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Total", totalSum / factor, 1000, Orientation.Up));
                var cleandStandortIDs = complexes.SelectMany(x => x.CleanedStandorte).ToList();
                var identifiedComplexStandorts = monthlyElectricityUsePerStandorts.Where(x => cleandStandortIDs.Contains(x.CleanedStandort));

                var identifiedmonthlygas = identifiedComplexStandorts.Sum(x => x.YearlyFernwaermeUse);
                arr1.AddEntry(new SankeyEntry("MonthlyElectricityUseNetz", identifiedmonthlygas / factor * -1, 1000, Orientation.Down));
                arrows.Add(arr1);
            }

            {
                var arr1 = new SingleSankeyArrow("Fernwärme_Verbrauch_Nach_BuildingWithGeoCoords", 500, MyStage, SequenceNumber, Name, Services.Logger, slice);
                var rfe = new ResultFileEntry(section, sectionDescription, arr1.FullPngFileName(), arr1.ArrowName, "", Constants.PresentSlice, MyStage);
                rfe.Save();
                var totalSum = monthlyElectricityUsePerStandorts.Select(x => x.YearlyFernwaermeUse).Sum();
                const double factor = 1_000_000;
                arr1.AddEntry(new SankeyEntry("Fernwärmeverbrauch gesamt", totalSum / factor, 500, Orientation.Straight));
                var cleandStandortIDs = complexes.Where(x => x.Coords.Count > 0).SelectMany(x => x.CleanedStandorte).ToList();
                var identifiedComplexStandorts = monthlyElectricityUsePerStandorts.Where(x => cleandStandortIDs.Contains(x.CleanedStandort));
                var identifiedmonthlyElectricityUseNetz = identifiedComplexStandorts.Sum(x => x.YearlyFernwaermeUse);
                arr1.AddEntry(new SankeyEntry("Fernwärmeverbrauch in\\n Gebäuden mit Geokoordinaten", identifiedmonthlyElectricityUseNetz / factor * -1, 200, Orientation.Straight));

                var notidentified = monthlyElectricityUsePerStandorts.Where(x => !cleandStandortIDs.Contains(x.CleanedStandort));
                var notidentifiedmonthlyElectricityUseNetz = notidentified.Sum(x => x.YearlyFernwaermeUse);
                arr1.AddEntry(new SankeyEntry("Fernwärmeverbrauch in nicht\\n zuordenbaren Gebäuden", notidentifiedmonthlyElectricityUseNetz / factor * -1, 200, Orientation.Up));

                arrows.Add(arr1);
            }
            return arrows;
        }

    }
}

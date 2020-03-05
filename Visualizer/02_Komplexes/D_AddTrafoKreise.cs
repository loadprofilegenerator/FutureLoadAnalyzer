using System;
using System.Linq;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Dst;
using Data.DataModel.Src;
using JetBrains.Annotations;
using Visualizer.Sankey;

namespace BurgdorfStatistics._02_Komplexes {
    // ReSharper disable once InconsistentNaming
    public class D_AddTrafoKreise : RunableWithBenchmark {
        public D_AddTrafoKreise([NotNull] ServiceRepository services)
            : base(nameof(D_AddTrafoKreise), Stage.Complexes, 4, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            Log(MessageType.Info, "Starting to add trafoKreisInfo");
            var dbsrc = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbdst = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var trafokreise = dbsrc.Fetch<TrafoKreisImport>();
            var buildingComplexes = dbdst.Fetch<BuildingComplex>();
            dbdst.BeginTransaction();
            Log(MessageType.Info, "Using Trafokreis data");
            var addedegids = 0;
            var addedTrafoKreise = 0;
            var totalTrafoKreise = 0;
            foreach (var tk in trafokreise) {
                if (!string.IsNullOrWhiteSpace(tk.DESCRIPTIO)) {
                    totalTrafoKreise++;
                }

                if (tk.U_OBJ_ID_I == null) {
                    continue;
                }

                var complexes = buildingComplexes.Where(x => x.GebäudeObjectIDs.Contains(tk.U_OBJ_ID_I.Value)).ToList();
                if (complexes.Count == 0) {
                    //new id that I dont have yet
                    continue;
                }

                if (complexes.Count > 1) {
                    //new id that I dont have yet
                    throw new Exception("more than one complex with the same gebäudeid");
                }

                var complex = complexes[0];
                //fehlende egids einlesen
                if (tk.U_EGID_ISE != null && tk.U_EGID_ISE != 0 && !complex.EGids.Contains(tk.U_EGID_ISE.Value)) {
                    complex.EGids.Add(tk.U_EGID_ISE.Value);
                    addedegids++;
                }

                var tkDesc = tk.DESCRIPTIO.Trim();
                if (!complex.TrafoKreise.Contains(tkDesc)) {
                    complex.TrafoKreise.Add(tkDesc);
                }

                if (!string.IsNullOrWhiteSpace(tk.u_Nr_Dez_E)) {
                    complex.ErzeugerIDs.Add(tk.u_Nr_Dez_E);
                }

                var xkoord = tk.HKOORD;
                var ykoord = tk.VKOORD;
                var addKoord = true;
                foreach (var coord in complex.LocalnetCoords) {
                    if (Math.Abs(coord.X - xkoord) < 0.0000001 && Math.Abs(ykoord - coord.Y) < 0.000001) {
                        addKoord = false;
                    }
                }

                if (addKoord) {
                    complex.LocalnetCoords.Add(new GeoCoord(xkoord, ykoord));
                }

                dbdst.Save(complex);
                addedTrafoKreise++;
            }

            dbdst.CompleteTransaction();
            //merging
            var merger = new ComplexMerger(SqlConnection, Services.MyLogger);
            merger.MergeBuildingComplexesAsNeeded();
            Log(MessageType.Info, "Merged from " + merger.BeginCount + " to " + merger.EndCount, "A_CreateComplexeService");

            Log(MessageType.Info, "Added trafokreis data");
            Log(MessageType.Info, "Added EGids: " + addedegids);
            Log(MessageType.Info, "Added trafokreise: " + addedTrafoKreise + "/" + totalTrafoKreise);
            dbdst.CloseSharedConnection();
        }

        protected override void RunChartMaking()
        {
            var dbComplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var complexes = dbComplex.Fetch<BuildingComplex>();
            MakeLocalnetCoordSankey(Constants.PresentSlice);

            void MakeLocalnetCoordSankey(ScenarioSliceParameters slice)
            {
                var ssa = new SingleSankeyArrow("ComplexesWithLocalnetCoords", 1000,
                    MyStage, SequenceNumber, Name, Services.Logger, slice);
                ssa.AddEntry(new SankeyEntry("Complexes", complexes.Count, 5000, Orientation.Straight));
                var complexesWithBoth = complexes.Count(x => x.LocalnetCoords.Count > 0 && x.Coords.Count > 0);
                var complexesWithOnlyLocalnet = complexes.Count(x => x.LocalnetCoords.Count > 0 && x.Coords.Count == 0);
                var complexesWithOnlyGWR = complexes.Count(x => x.LocalnetCoords.Count == 0 && x.Coords.Count > 0);
                var complexesWithNone = complexes.Count(x => x.LocalnetCoords.Count == 0 && x.Coords.Count == 0);
                ssa.AddEntry(new SankeyEntry("GWR & Localnet", complexesWithBoth * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("GWR ", complexesWithOnlyGWR * -1, 2000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Localnet Daten Verfügbar", complexesWithOnlyLocalnet * -1, 2000, Orientation.Down));
                ssa.AddEntry(new SankeyEntry("none  ", complexesWithNone * -1, 2000, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
                /*
                RGB GetColor(House h)
                {
                    if (h.GebäudeObjectIDs.Count > 0 && h.TotalSolarEnergyPotential > 0)
                    {
                        return new RGB(255, 0, 0);
                    }

                    if (h.GebäudeObjectIDs.Count > 0)
                    {
                        return new RGB(0, 0, 255);
                    }
                    return new RGB(0, 255, 0);

                }
                var mapPoints = complexes.Select(x => x.GetMapPoint(GetColor)).ToList();
                var filename = MakeAndRegisterFullFilename("MapPVWithLocalnet.svg", Name, "");
                List<MapLegendEntry> legendEntries = new List<MapLegendEntry>();
                legendEntries.Add(new MapLegendEntry("Localnet und Solar", 255, 0, 0));
                legendEntries.Add(new MapLegendEntry("Localnet", 0, 0, 255));
                legendEntries.Add(new MapLegendEntry("Rest", 0, 255, 0));
                Services.PlotMaker.MakeMapDrawer(filename, Name, mapPoints, legendEntries);
                */
            }
        }
    }
}
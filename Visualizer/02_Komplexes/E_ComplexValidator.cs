using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Dst;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;

namespace BurgdorfStatistics._02_Komplexes {
    // ReSharper disable once InconsistentNaming
    public class E_ComplexValidator : RunableWithBenchmark {
        public E_ComplexValidator([NotNull] ServiceRepository services) : base(nameof(E_ComplexValidator), Stage.Complexes, 5, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            Log(MessageType.Info, "Valdiating Complex Standorte");
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbComplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var localnet = dbRaw.Fetch<Localnet>();
            var buildingComplexes = dbComplex.Fetch<BuildingComplex>();
            var standorteByGebäudeID = new Dictionary<int, List<string>>();
            var gebäudeIDByStandort = new Dictionary<string, List<int>>();
            dbComplex.BeginTransaction();
            foreach (var lo in localnet) {
                var id = lo.ObjektIDGebäude ?? throw new Exception("Was null");
                var standort = lo.Objektstandort;
                if (string.IsNullOrWhiteSpace(standort)) {
                    continue;
                }

                if (!standorteByGebäudeID.ContainsKey(id)) {
                    standorteByGebäudeID.Add(id, new List<string>());
                }

                if (!standorteByGebäudeID[id].Contains(standort)) {
                    standorteByGebäudeID[id].Add(standort);
                }

                if (!gebäudeIDByStandort.ContainsKey(standort)) {
                    gebäudeIDByStandort.Add(standort, new List<int>());
                }

                if (!gebäudeIDByStandort[standort].Contains(id)) {
                    gebäudeIDByStandort[standort].Add(id);
                }
            }

            var addedStandorte = 0;
            var addedIds = 0;
            while (addedStandorte > 0 || addedIds > 0) {
                foreach (var complex in buildingComplexes) {
                    foreach (var cid in complex.GebäudeObjectIDs) {
                        var cStandorte = complex.ObjektStandorte;
                        var lStandorte = standorteByGebäudeID[cid];
                        if (!Constants.ScrambledEquals(cStandorte, lStandorte)) {
                            foreach (var s in lStandorte) {
                                if (!cStandorte.Contains(s)) {
                                    cStandorte.Add(s);
                                    addedStandorte++;
                                }
                            }
                        }
                    }

                    foreach (var cstandort in complex.ObjektStandorte) {
                        var cids = complex.GebäudeObjectIDs;
                        var lIds = gebäudeIDByStandort[cstandort];
                        if (!Constants.ScrambledEquals(cids, lIds)) {
                            foreach (var id in lIds) {
                                if (!cids.Contains(id)) {
                                    cids.Add(id);
                                    addedIds++;
                                }
                            }
                        }
                    }
                }
            }

            Log(MessageType.Info, "Merging as needed", "A_CreateComplexeService");
            var merger = new ComplexMerger(SqlConnection, Services.MyLogger);
            merger.MergeBuildingComplexesAsNeeded();
            Log(MessageType.Info, "Merged from " + merger.BeginCount + " to " + merger.EndCount, "A_CreateComplexeService");

            foreach (var complex in buildingComplexes) {
                dbComplex.Save(complex);
            }

            Log(MessageType.Info, "Added Ids: " + addedIds);
            Log(MessageType.Info, "Added Standorte: " + addedStandorte);
            dbComplex.CompleteTransaction();
        }

        protected void MakeComplexSankeys([NotNull] [ItemNotNull] List<BuildingComplex> complexes)
        {
            var coords = 0;
            foreach (var complex in complexes)
            {
                coords += complex.Coords.Count;
            }

            Log(MessageType.Info, "UsedCoords = " + coords);
            var arr1 = MakeArr1(complexes);
            var arr2 = MakeArr2(complexes);
            const string filename = "complexcount_sankeygenerator.py";
            const string pngFilename = "complexCount_sankey.png";
            string dstPyFilename = MakeAndRegisterFullFilename(filename, "", "", Constants.PresentSlice);
            var sw = new StreamWriter(dstPyFilename);
            sw.WriteLine("import matplotlib.pyplot as plt");
            sw.WriteLine("from matplotlib.sankey import Sankey");
            sw.WriteLine("sankey = Sankey(gap=1000)"); //head_angle = 180
            sw.WriteLine("# block A");
            sw.WriteLine("sankey.add(flows =[" + arr1.GetFlows() + "],");
            sw.WriteLine("orientations =[" + arr1.GetDirections() + "],");
            sw.WriteLine("labels =[" + arr1.GetNames() + "],");
            sw.WriteLine("pathlengths =[" + arr1.GetPathLengths() + "],");
            sw.WriteLine("trunklength = 5000, alpha = 0.1)");
            //arrow two

            sw.WriteLine("# block A");
            sw.WriteLine("sankey.add(flows =[" + arr2.GetFlows() + "],");
            sw.WriteLine("orientations =[" + arr2.GetDirections() + "],");
            sw.WriteLine("labels =[" + arr2.GetNames() + "],");
            sw.WriteLine("pathlengths =[" + arr2.GetPathLengths() + "],");
            sw.WriteLine("prior = 0, connect = (4, 0),");
            sw.WriteLine("trunklength = 5000, alpha =0.1)");
            sw.WriteLine("plt.box(False)");
            sw.WriteLine("diagrams = sankey.finish()");
            sw.WriteLine("plt.savefig('" + pngFilename + "')");
            sw.Close();
             MakeAndRegisterFullFilename(pngFilename, Name, "", Constants.PresentSlice);

#pragma warning disable CC0022 // Should dispose object
            var process = new Process();
#pragma warning restore CC0022 // Should dispose object
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = GetThisTargetDirectory(),
                FileName = "c:\\python37\\python.exe",
                Arguments = filename,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            process.StartInfo = startInfo;
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                Log(MessageType.Info, line ?? throw new InvalidOperationException());
                // do something with line
            }

            while (!process.StandardError.EndOfStream)
            {
                var line = process.StandardError.ReadLine();
                Log(MessageType.Info, line ?? throw new InvalidOperationException());
                // do something with line
            }

            //var msc = new MySqlConnection();
            //msc.RecreateTable<BuildingComplex>(MyStage, Constants.PresentSlice);
            //var filtered = complexes.Where(x => x.GebäudeObjectIDs.Count > 0 && x.Coords.Count == 0).ToList();
/*            var dbvisual = SqlConnection.GetDatabaseConnection(Stage.PresentVisualisation, Constants.PresentSlice).Database;
            dbvisual.BeginTransaction();
            foreach (var buildingComplex in filtered)
            {
                buildingComplex.ComplexID = 0;
                dbvisual.Save(buildingComplex);
            }

            dbvisual.CompleteTransaction();*/
        }

        [NotNull]
        private SingleSankeyArrow MakeArr2([ItemNotNull] [NotNull] List<BuildingComplex> complexes)
        {
            var slice = Constants.PresentSlice;
            var outcounts = new Dictionary<string, int>();
            var arr2 = new SingleSankeyArrow("Komplexe", 1000, MyStage, SequenceNumber, Name, Services.Logger, slice);
            arr2.AddEntry(new SankeyEntry("Komplexe", complexes.Count, 1000, Orientation.Straight));
            foreach (Variants variant1 in Enum.GetValues(typeof(Variants)))
            {
                // addresses
                string fullDesc;
                List<BuildingComplex> l1Complexes;
                if (variant1 == Variants.None)
                {
                    l1Complexes = complexes.Where(x => x.Adresses.Count == 0).ToList();
                    fullDesc = "keine Adressen,\\n";
                }
                else
                {
                    l1Complexes = complexes.Where(x => x.Adresses.Count > 0).ToList();
                    fullDesc = "mindestens eine Addresse,\\n";
                }

                foreach (Variants variant2 in Enum.GetValues(typeof(Variants)))
                {
                    //geocoords
                    List<BuildingComplex> l2Complexes;
                    var l2Desc = fullDesc;
                    if (variant2 == Variants.None)
                    {
                        l2Complexes = l1Complexes.Where(x => x.Coords.Count == 0).ToList();
                        l2Desc += "keine Koord.,\\n";
                    }
                    else
                    {
                        l2Complexes = l1Complexes.Where(x => x.Coords.Count > 0).ToList();
                        l2Desc += "mindestens eine Koord.,\\n";
                    }

                    foreach (Variants variant3 in Enum.GetValues(typeof(Variants)))
                    {
                        //localnetGebäudeIDs
                        List<BuildingComplex> l3Complexes;
                        var l3Desc = l2Desc;
                        if (variant3 == Variants.None)
                        {
                            l3Complexes = l2Complexes.Where(x => x.GebäudeObjectIDs.Count == 0).ToList();
                            l3Desc += "keine GebäudeIds";
                            outcounts.Add(l3Desc, l3Complexes.Count);
                        }
                        else
                        {
                            l3Complexes = l2Complexes.Where(x => x.GebäudeObjectIDs.Count > 0).ToList();
                            l3Desc += "mindestens eine GebäudeId";
                            outcounts.Add(l3Desc, l3Complexes.Count);
                        }
                    }
                }
            }

            var pathLength = 1000;

            foreach (var pair in outcounts)
            {
                if (pair.Value > 0)
                {
                    if (pair.Value > 3000)
                    {
                        arr2.AddEntry(new SankeyEntry(pair.Key, pair.Value * -1, 3000, Orientation.Straight));
                    }
                    else
                    {
                        pathLength += 4000;
                        arr2.AddEntry(new SankeyEntry(pair.Key, pair.Value * -1, pathLength, Orientation.Up));
                    }
                }
            }

            return arr2;
        }

        [NotNull]
        private SingleSankeyArrow MakeArr1([ItemNotNull] [NotNull] List<BuildingComplex> complexes)
        {
            var slice = Constants.PresentSlice;
            //write python
            var arr1 = new SingleSankeyArrow("komplexe", 1000, MyStage, SequenceNumber, Name, Services.Logger, slice);
            var sources = complexes.Select(x => x.SourceOfThisEntry).Distinct().ToList();
            var length = 2000;
            foreach (var entry in sources)
            {
                var count = complexes.Count(x => x.SourceOfThisEntry == entry);
                if (count > 3000)
                {
                    arr1.AddEntry(new SankeyEntry(entry.ToString(), count, length, Orientation.Straight));
                }
                else
                {
                    arr1.AddEntry(new SankeyEntry(entry.ToString(), count, length, Orientation.Up));
                }

                length += 2000;
            }

            arr1.AddEntry(new SankeyEntry("Komplexe", complexes.Count * -1, 1000, Orientation.Straight));
            return arr1;
        }

        /* public enum Orientation {
             Up = -1,
             Straight= 0,
             Down=1
         }
 
         public class SingleSankeyArrow {
             [ItemNotNull]
             [NotNull]
             private List<SankeyEntry> Entries { get; } = new List<SankeyEntry>();
 
             [NotNull]
             public string GetNames()
             {
                 string s = "";
                 foreach (SankeyEntry entry in Entries)
                 {
                     s += "'"+  entry.Name + "', ";
                 }
                 return s.Substring(0, s.Length - 2);
             }
 
             [NotNull]
             public string GetPathLengths()
             {
                 string s = "";
                 foreach (SankeyEntry entry in Entries) {
                     s += entry.PathLenght + ", ";
                 }
                 return s.Substring(0, s.Length - 2);
             }
             [NotNull]
             public string GetFlows()
             {
                 string s = "";
                 foreach (SankeyEntry entry in Entries)
                 {
                     s += entry.Value + ", ";
                 }
                 return s.Substring(0, s.Length - 2);
             }
 
             [NotNull]
             public string GetDirections()
             {
                 string s = "";
                 foreach (SankeyEntry entry in Entries)
                 {
                     s += (int)entry.Orientation+ ", ";
                 }
                 return s.Substring(0, s.Length - 2);
             }
 
             public void AddEntry([NotNull] SankeyEntry entry)
             {
                 Entries.Add(entry);
             }
 
         }
         public class SankeyEntry {
             public SankeyEntry([NotNull] string name, double value, double pathLenght, Orientation orientation)
             {
                 Name = name;
                 Value = value;
                 PathLenght = pathLenght;
                 Orientation = orientation;
             }
 
             [NotNull]
             public string Name { get; set; }
             public double Value { get; set; }
             public double PathLenght { get; set; }
             public Orientation Orientation { get; set; }
         }*/

        private enum Variants
        {
            None,

            // ReSharper disable once UnusedMember.Local
            Some
        }
        protected override void RunChartMaking()
        {
            var dbComplexEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData, Constants.PresentSlice);
            var complexBuildingDatas = dbComplexEnergy.Database.Fetch<ComplexBuildingData>();
            var dbComplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var complexes = dbComplex.Fetch<BuildingComplex>();
            Log(MessageType.Info, "Loaded complex building data: " + complexBuildingDatas.Count);

            MakeComplexSankeys(complexes);
            var pointsNumberOfMergedEntries = new List<MapPoint>();
            var anzahlWohnungen = new List<MapPoint>();
            var calcEhzww = new List<MapPoint>();
            var calcWhzww = new List<MapPoint>();
            var missingEntries = 0;
            foreach (var data in complexBuildingDatas) {
                var complex = complexes.Single(x => data.ComplexName == x.ComplexName);
                if (complex.Coords.Count > 0) {
                    foreach (var coord in complex.Coords) {
                        pointsNumberOfMergedEntries.Add(new MapPoint(coord.X, coord.Y, data.NumberOfMergedEntries, data.NumberOfMergedEntries * 10));
                        anzahlWohnungen.Add(new MapPoint(coord.X, coord.Y, data.AnzahlWohnungenBern, (int)(Math.Max(Math.Log(data.AnzahlWohnungenBern), 1) * 10)));
                        calcEhzww.Add(new MapPoint(coord.X, coord.Y, data.calc_ehzww, 10));
                        calcWhzww.Add(new MapPoint(coord.X, coord.Y, data.calc_whzww, 10));
                    }
                }
                else {
                    missingEntries++;
                }
            }

            Log(MessageType.Info, "Number of missing Entries: " + missingEntries + "/" + complexBuildingDatas.Count);
            const string sectionDescription = "";
            var fullName = MakeAndRegisterFullFilename("MapNumberOfMergedEntries.svg", Name, sectionDescription, Constants.PresentSlice);
            Services.PlotMaker.MakeMapDrawer(fullName, "", pointsNumberOfMergedEntries, new List<MapLegendEntry>(), MyStage);


            var fullName2 = MakeAndRegisterFullFilename("MapAnzahlWohnungen.svg", Name, sectionDescription, Constants.PresentSlice);
            Services.PlotMaker.MakeMapDrawer(fullName2, "", anzahlWohnungen, new List<MapLegendEntry>(), MyStage);

            var fullName3 = MakeAndRegisterFullFilename("Mapcalc_ehzww.svg", Name, sectionDescription, Constants.PresentSlice);
            Services.PlotMaker.MakeMapDrawer(fullName3, "", calcEhzww, new List<MapLegendEntry>(), MyStage);

            var fullName4 = MakeAndRegisterFullFilename("Mapcalc_whzww.svg", Name, sectionDescription, Constants.PresentSlice);
            Services.PlotMaker.MakeMapDrawer(fullName4, "", calcWhzww, new List<MapLegendEntry>(), MyStage);
            Log(MessageType.Info, "NumberOfMergedEntries written");
        }
    }
}
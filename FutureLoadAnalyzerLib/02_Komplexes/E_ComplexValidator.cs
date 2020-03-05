using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data.DataModel.Dst;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.Sankey;

namespace FutureLoadAnalyzerLib._02_Komplexes {
    // ReSharper disable once InconsistentNaming
    public class E_ComplexValidator : RunableWithBenchmark {
        public E_ComplexValidator([NotNull] ServiceRepository services) : base(nameof(E_ComplexValidator), Stage.Complexes, 5, services, true)
        {
        }

        protected void MakeComplexSankeys([NotNull] [ItemNotNull] List<BuildingComplex> complexes)
        {
            var coords = 0;
            foreach (var complex in complexes) {
                coords += complex.Coords.Count;
            }

            Info("UsedCoords = " + coords);
            var arr1 = MakeArr1(complexes);
            var arr2 = MakeArr2(complexes);
            const string filename = "complexcount_sankeygenerator.py";
            const string pngFilename = "complexCount_sankey.png";
            string dstPyFilename = MakeAndRegisterFullFilename(filename, Constants.PresentSlice);
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
            MakeAndRegisterFullFilename(pngFilename, Constants.PresentSlice);

#pragma warning disable CC0022 // Should dispose object
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var process = new Process();
#pragma warning restore IDE0067 // Dispose objects before losing scope
#pragma warning restore CA2000 // Dispose objects before losing scope
#pragma warning restore CC0022 // Should dispose object
            var startInfo = new ProcessStartInfo {
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
            while (!process.StandardOutput.EndOfStream) {
                var line = process.StandardOutput.ReadLine();
                Info(line ?? throw new InvalidOperationException());
                // do something with line
            }

            while (!process.StandardError.EndOfStream) {
                var line = process.StandardError.ReadLine();
                Info(line ?? throw new InvalidOperationException());
                // do something with line
            }
        }

        protected override void RunActualProcess()
        {
            Debug("Valdiating Complex Standorte");
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbComplex = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var localnet = dbRaw.Fetch<Localnet>();
            var buildingComplexes = dbComplex.Fetch<BuildingComplex>();
            var standorteByGebäudeID = new Dictionary<int, List<string>>();
            var gebäudeIDByStandort = new Dictionary<string, List<int>>();
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

            Debug("Merging as needed");
            var merger = new ComplexMerger(Services, MyStage);
            merger.MergeBuildingComplexesAsNeeded();
            Info("Merged from " + merger.BeginCount + " to " + merger.EndCount, "A_CreateComplexeService");
            dbComplex.BeginTransaction();
            foreach (var complex in buildingComplexes) {
                dbComplex.Save(complex);
            }

            Debug("Added Ids: " + addedIds);
            Debug("Added Standorte: " + addedStandorte);
            dbComplex.CompleteTransaction();
        }

        protected override void RunChartMaking()
        {
            var dbComplex = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var complexes = dbComplex.Fetch<BuildingComplex>();

            MakeComplexSankeys(complexes);
        }

        [NotNull]
        private SingleSankeyArrow MakeArr1([ItemNotNull] [NotNull] List<BuildingComplex> complexes)
        {
            var slice = Constants.PresentSlice;
            //write python
            var arr1 = new SingleSankeyArrow("komplexe", 1000, MyStage, SequenceNumber, Name, slice, Services);
            var sources = complexes.Select(x => x.SourceOfThisEntry).Distinct().ToList();
            var length = 2000;
            foreach (var entry in sources) {
                var count = complexes.Count(x => x.SourceOfThisEntry == entry);
                if (count > 3000) {
                    arr1.AddEntry(new SankeyEntry(entry.ToString(), count, length, Orientation.Straight));
                }
                else {
                    arr1.AddEntry(new SankeyEntry(entry.ToString(), count, length, Orientation.Up));
                }

                length += 2000;
            }

            arr1.AddEntry(new SankeyEntry("Komplexe", complexes.Count * -1, 1000, Orientation.Straight));
            return arr1;
        }

        [NotNull]
        private SingleSankeyArrow MakeArr2([ItemNotNull] [NotNull] List<BuildingComplex> complexes)
        {
            var slice = Constants.PresentSlice;
            var outcounts = new Dictionary<string, int>();
            var arr2 = new SingleSankeyArrow("Komplexe", 1000, MyStage, SequenceNumber, Name, slice, Services);
            arr2.AddEntry(new SankeyEntry("Komplexe", complexes.Count, 1000, Orientation.Straight));
            foreach (Variants variant1 in Enum.GetValues(typeof(Variants))) {
                // addresses
                string fullDesc;
                List<BuildingComplex> l1Complexes;
                if (variant1 == Variants.None) {
                    l1Complexes = complexes.Where(x => x.Adresses.Count == 0).ToList();
                    fullDesc = "keine Adressen,\\n";
                }
                else {
                    l1Complexes = complexes.Where(x => x.Adresses.Count > 0).ToList();
                    fullDesc = "mindestens eine Addresse,\\n";
                }

                foreach (Variants variant2 in Enum.GetValues(typeof(Variants))) {
                    //geocoords
                    List<BuildingComplex> l2Complexes;
                    var l2Desc = fullDesc;
                    if (variant2 == Variants.None) {
                        l2Complexes = l1Complexes.Where(x => x.Coords.Count == 0).ToList();
                        l2Desc += "keine Koord.,\\n";
                    }
                    else {
                        l2Complexes = l1Complexes.Where(x => x.Coords.Count > 0).ToList();
                        l2Desc += "mindestens eine Koord.,\\n";
                    }

                    foreach (Variants variant3 in Enum.GetValues(typeof(Variants))) {
                        //localnetGebäudeIDs
                        List<BuildingComplex> l3Complexes;
                        var l3Desc = l2Desc;
                        if (variant3 == Variants.None) {
                            l3Complexes = l2Complexes.Where(x => x.GebäudeObjectIDs.Count == 0).ToList();
                            l3Desc += "keine LocalnetID";
                            outcounts.Add(l3Desc, l3Complexes.Count);
                        }
                        else {
                            l3Complexes = l2Complexes.Where(x => x.GebäudeObjectIDs.Count > 0).ToList();
                            l3Desc += "mindestens eine LocalnetID";
                            outcounts.Add(l3Desc, l3Complexes.Count);
                        }
                    }
                }
            }

            var pathLength = 1000;

            foreach (var pair in outcounts) {
                if (pair.Value > 0) {
                    if (pair.Value > 3000) {
                        arr2.AddEntry(new SankeyEntry(pair.Key, pair.Value * -1, 3000, Orientation.Straight));
                    }
                    else {
                        pathLength += 4000;
                        arr2.AddEntry(new SankeyEntry(pair.Key, pair.Value * -1, pathLength, Orientation.Up));
                    }
                }
            }

            return arr2;
        }

        private enum Variants {
            None,

            // ReSharper disable once UnusedMember.Local
            Some
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Common;
using Common.Config;
using Common.Database;
using Common.Steps;
using Data.DataModel.Dst;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using OfficeOpenXml;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._02_Komplexes {
    public class ReadComplexMergeTester : UnitTestBase {
        public ReadComplexMergeTester([NotNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public void Run()
        {
            ServiceRepository services = new ServiceRepository(null, null, Logger, Config, null);
            ComplexMerger cm = new ComplexMerger(services, Stage.Complexes);
            var r = cm.ReadComplexesToMergeList();
            Assert.Single(r);
        }
    }

    internal class ComplexMerger : BasicLoggable {
        [NotNull] private readonly ServiceRepository _services;

        public ComplexMerger([NotNull] ServiceRepository services, Stage myStage) : base(services.Logger, myStage, nameof(ComplexMerger)) =>
            _services = services;

        public int BeginCount { get; set; }
        public int EndCount { get; set; }

        public void MergeBuildingComplexesAsNeeded()
        {
            var dbComplex = _services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var complexes = dbComplex.Fetch<BuildingComplex>();
            BeginCount = complexes.Count;
            var i = 0;
            List<ComplexesToMerge> manualMergeList = ReadComplexesToMergeList();
            List<string> manualMergeNames = manualMergeList.Select(x => x.ComplexName1).ToList();
            manualMergeNames.AddRange(manualMergeList.Select(x => x.ComplexName2));
            var manualMergeHash = manualMergeNames.Distinct().ToHashSet();
            while (MergeOnceBuildingComplexesAsNeeded1()) {
                Debug("Merging Iteration:" + i++);
            }

            while (MergeOnceBuildingComplexesAsNeeded2(manualMergeList, manualMergeHash)) {
                Debug("Merging Iteration:" + i++);
            }

            complexes = dbComplex.Fetch<BuildingComplex>();
            EndCount = complexes.Count;
        }

        [NotNull]
        [ItemNotNull]
        public List<ComplexesToMerge> ReadComplexesToMergeList()
        {
            string path = Path.Combine(_services.RunningConfig.Directories.BaseUserSettingsDirectory, "Corrections", "ComplexesToMerge.xlsx");
            ExcelPackage ep = new ExcelPackage(new FileInfo(path));
            int row = 2;
            ExcelWorksheet ws = ep.Workbook.Worksheets[1];
            List<ComplexesToMerge> ctm = new List<ComplexesToMerge>();
            while (ws.Cells[row, 1].Value != null) {
                string c1 = (string)ws.Cells[row, 1].Value;
                string c2 = (string)ws.Cells[row, 2].Value;
                ctm.Add(new ComplexesToMerge(c1.Trim(), c2.Trim()));
                row++;
            }

            ep.Dispose();
            return ctm;
        }

        private static bool FindManualMerges([NotNull] BuildingComplex complex1,
                                             [NotNull] BuildingComplex complex2,
                                             [NotNull] [ItemNotNull] List<ComplexesToMerge> manualMerges,
                                             [NotNull] [ItemNotNull] HashSet<string> manualMergeHash)
        {
            if (!manualMergeHash.Contains(complex1.ComplexName)) {
                return false;
            }

            foreach (ComplexesToMerge merge in manualMerges) {
                if (merge.ComplexName1 == complex1.ComplexName && merge.ComplexName2 == complex2.ComplexName) {
                    merge.IsProcessed = true;
                    return true;
                }

                if (merge.ComplexName1 == complex2.ComplexName && merge.ComplexName2 == complex1.ComplexName) {
                    merge.IsProcessed = true;
                    return true;
                }
            }

            return false;
        }

        private static bool FindOverlapInAdresses([NotNull] BuildingComplex complex1, [NotNull] BuildingComplex complex2)
        {
            foreach (var complex2Adress in complex2.CleanedAdresses) {
                if (complex1.CleanedAdresses.Contains(complex2Adress)) {
                    return true;
                }
            }

            return false;
        }


        private static bool FindOverlapInEGids([NotNull] BuildingComplex complex1, [NotNull] BuildingComplex complex2)
        {
            foreach (var eGid2 in complex2.EGids) {
                if (complex1.EGids.Contains(eGid2)) {
                    return true;
                }
            }

            return false;
        }

        private static bool FindOverlapInGebäudeIDs([NotNull] BuildingComplex complex1, [NotNull] BuildingComplex complex2)
        {
            foreach (var complex2Adress in complex2.GebäudeObjectIDs) {
                if (complex1.GebäudeObjectIDs.Contains(complex2Adress)) {
                    return true;
                }
            }

            return false;
        }

        private static bool FindOverlapInStandorte([NotNull] BuildingComplex complex1, [NotNull] BuildingComplex complex2)
        {
            foreach (var complex2Adress in complex2.ObjektStandorte) {
                if (complex1.ObjektStandorte.Contains(complex2Adress)) {
                    return true;
                }
            }

            return false;
        }

        private bool MergeOnceBuildingComplexesAsNeeded1()
        {
            var dbComplex = _services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var complexes = dbComplex.Fetch<BuildingComplex>();
            if (BeginCount == 0) {
                BeginCount = complexes.Count;
            }

            dbComplex.BeginTransaction();
            foreach (var complex1 in complexes) {
                foreach (var complex2 in complexes) {
                    if (complex1 == complex2) {
                        continue;
                    }

                    if (!FindOverlapInEGids(complex1, complex2) && !FindOverlapInAdresses(complex1, complex2) &&
                        !FindOverlapInStandorte(complex1, complex2) && !FindOverlapInGebäudeIDs(complex1, complex2)) {
                        continue;
                    }

                    var c1 = complex1;
                    var c2 = complex2;
                    PerformActualComplexMerge(c2, c1, dbComplex);
                    dbComplex.CompleteTransaction();
                    return true;
                }
            }

            dbComplex.CompleteTransaction();
            return false;
        }

        private bool MergeOnceBuildingComplexesAsNeeded2([NotNull] [ItemNotNull] List<ComplexesToMerge> manualMerges,
                                                         [NotNull] [ItemNotNull] HashSet<string> manualMergeHash)
        {
            var dbComplex = _services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var complexes = dbComplex.Fetch<BuildingComplex>();
            if (BeginCount == 0) {
                BeginCount = complexes.Count;
            }

            dbComplex.BeginTransaction();
            foreach (var complex1 in complexes) {
                foreach (var complex2 in complexes) {
                    if (complex1 == complex2) {
                        continue;
                    }

                    if (!FindManualMerges(complex1, complex2, manualMerges, manualMergeHash)) {
                        continue;
                    }

                    var c1 = complex1;
                    var c2 = complex2;
                    PerformActualComplexMerge(c2, c1, dbComplex);
                    dbComplex.CompleteTransaction();
                    return true;
                }
            }

            dbComplex.CompleteTransaction();
            return false;
        }

        private static void PerformActualComplexMerge([NotNull] BuildingComplex c2, [NotNull] BuildingComplex c1, [NotNull] MyDb dbComplex)
        {
            foreach (var c2EGid in c2.EGids) {
                if (!c1.EGids.Contains(c2EGid)) {
                    c1.EGids.Add(c2EGid);
                }
            }

            foreach (var c2Adress in c2.Adresses) {
                if (!c1.Adresses.Contains(c2Adress)) {
                    c1.AddAdress(c2Adress);
                }
            }

            foreach (var c2Coord in c2.Coords) {
                if (!c1.Coords.Contains(c2Coord)) {
                    c1.AddCoord(c2Coord);
                }
            }

            foreach (var c2Coord in c2.LocalnetCoords) {
                if (!c1.LocalnetCoords.Contains(c2Coord)) {
                    c1.AddLocalCoord(c2Coord);
                }
            }

            foreach (var c2ID in c2.GebäudeObjectIDs) {
                if (!c1.GebäudeObjectIDs.Contains(c2ID)) {
                    c1.AddGebäudeID(c2ID);
                }
            }

            foreach (var c2ID in c2.ObjektStandorte) {
                if (!c1.ObjektStandorte.Contains(c2ID)) {
                    c1.ObjektStandorte.Add(c2ID);
                }
            }

            foreach (var c2Tk in c2.TrafoKreise) {
                if (!c1.TrafoKreise.Contains(c2Tk)) {
                    c1.TrafoKreise.Add(c2Tk);
                }
            }

            dbComplex.Save(c1);
            dbComplex.Delete(c2);
        }
    }
}
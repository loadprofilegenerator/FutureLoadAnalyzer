using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Dst;
using Data.DataModel.Src;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._02_Komplexes {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class A_CreateComplexes : RunableWithBenchmark {
        public A_CreateComplexes([NotNull] ServiceRepository services) : base(nameof(A_CreateComplexes), Stage.Complexes, 1, services, false)
        {
            DevelopmentStatus.Add("convert all coordinates to the same system");
            DevelopmentStatus.Add("do nearness matching for all entries");
        }

        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        public void CheckKomplexes()
        {
            var dbdst = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var complexes = dbdst.Fetch<BuildingComplex>();
            foreach (var complex in complexes) {
                if (complex.ComplexGuid == null) {
                    throw new Exception("guid was null");
                }
            }
        }

        public void P04_AddTrafoStationData()
        {
            var dbsrc = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbdst = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var trafokreise = dbsrc.Fetch<TrafoKreisImport>();
            var buildingComplexes = dbdst.Fetch<BuildingComplex>();
            dbdst.BeginTransaction();
            Info("Using Trafokreis data");
            foreach (var tk in trafokreise) {
                //fehlende egids einlesen
                if (tk.U_EGID_ISE != null && tk.U_EGID_ISE != 0) {
                    var complex = buildingComplexes.FirstOrDefault(x =>
                        x.GebäudeObjectIDs.Contains(tk.U_OBJ_ID_I ?? throw new Exception("Value was null")));
                    if (complex != null && !complex.EGids.Contains(tk.U_EGID_ISE.Value)) {
                        complex.EGids.Add(tk.U_EGID_ISE.Value);
                        dbdst.Save(complex);
                    }
                }
            }

            dbdst.CompleteTransaction();
            Info("Energiebedarfsdaten: Saved every building from Energiebedarfsdaten as first complexes");
            dbdst.CloseSharedConnection();
        }

        protected override void RunActualProcess()
        {
            Debug("Starting creating complexes");
            Debug("Clearing tables");
            var dbdst = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            dbdst.RecreateTable<BuildingComplex>();
            dbdst.RecreateTable<BuildingComplexStandorte>();
            Debug("Energiebedarfsdaten");
            P01_StartWithEnergieBedarfsdaten();
            Debug("Feurungsstaetten");
            P02_AddFeuerungsstaettenAdressDataAsComplexes();
            Debug("GWRAdressen");
            P03_AddGwrAdressen();
            Debug("Trafokreis");
            P04_AddTrafoStationData();
            Debug("Merging as needed");
            var merger = new ComplexMerger(Services, MyStage);
            merger.MergeBuildingComplexesAsNeeded();
            Debug("Merged from " + merger.BeginCount + " to " + merger.EndCount);
            Debug("Finished");
            CheckKomplexes();
        }

        private void P01_StartWithEnergieBedarfsdaten()
        {
            var dbsrc = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbdst = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var energie = dbsrc.Fetch<EnergiebedarfsdatenBern>();
            dbdst.BeginTransaction();
            Debug("Energiebedarfsdaten: Using Energebedarfsdaten as foundation for forming the complexes");
            foreach (var energiebedarfsdatenBern in energie) {
                var bc = new BuildingComplex(Guid.NewGuid().ToString(), BuildingComplex.SourceOfEntry.BernData);
                bc.EGids.Add(energiebedarfsdatenBern.egid);
                dbdst.Save(bc);
            }

            dbdst.CompleteTransaction();
            Debug("Energiebedarfsdaten: Saved every building from Energiebedarfsdaten as first complexes");
            dbdst.CloseSharedConnection();
        }

        private void P02_AddFeuerungsstaettenAdressDataAsComplexes()
        {
            Debug("Feuerungsstätten:Starting to add adressen from feurungsstätten");
            var dbComplexes = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var complexes = dbComplexes.Fetch<BuildingComplex>();
            var feuerungsStaetten = dbRaw.Fetch<FeuerungsStaette>();

            Debug("Feuerungsstätten:Found " + feuerungsStaetten.Count);
            var vegid = -1000;
            var missingCount = 1;
            foreach (var staette in feuerungsStaetten) {
                if (staette.EGID == -99) {
                    staette.EGID = vegid--;
                    Debug("Feuringsstätte missing EGID #" + missingCount);
                    missingCount++;
                }
            }

            Debug("Feuerungsstätten: Total missing Count: " + missingCount);
            dbComplexes.BeginTransaction();
            Debug("Feuerungsstätten: Trying to add the adresses from the feurungsstätten to the building complexes");
            var newComplexesCreated = 0;
            var assignedToExactlyOneComplex = 0;
            var multiplePossibleComplexes = 0;
            foreach (var fs in feuerungsStaetten) {
                if (fs.EGID == null) {
                    throw new FlaException("fs.EGID != null");
                }

                var existingComplexes = complexes.Where(x => x.EGids.Contains(fs.EGID.Value)).ToList();
                BuildingComplex bc;
                if (existingComplexes.Count == 0) {
                    newComplexesCreated++;
                    bc = new BuildingComplex(Guid.NewGuid().ToString(), BuildingComplex.SourceOfEntry.Feuerungsstaetten);
                    bc.EGids.Add(fs.EGID.Value);
                    existingComplexes.Add(bc);
                }
                else {
                    //just use the first complex that is found to assign the adress
                    if (existingComplexes.Count == 1) {
                        assignedToExactlyOneComplex++;
                    }
                    else {
                        multiplePossibleComplexes++;
                    }

                    bc = existingComplexes[0];
                }

                var addresse = fs.Strasse + " " + fs.Hausnummer;
                bc.AddAdress(addresse);
                dbComplexes.Save(bc);
            }

            Debug("Feuerungsstätten: Newly created Complexes: " + newComplexesCreated);
            Debug("Feuerungsstätten: Assigned to exactly one complex: " + assignedToExactlyOneComplex);
            Debug("Feuerungsstätten: Assigned to exactly one complex out of multiple possible ones: " + multiplePossibleComplexes);
            dbComplexes.CompleteTransaction();
            dbComplexes.CloseSharedConnection();
            Debug("Feuerungsstätten:finished adding adressen from feurungsstätten");
        }


        private void P03_AddGwrAdressen()
        {
            Debug("GWRAdressen:adding adressen from gwr");
            var dbraw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var dbcomplex = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var complexes = dbcomplex.Fetch<BuildingComplex>();
            var gwrAdresses = dbraw.Fetch<GwrAdresse>();
            Debug("GWRAdressen:Found a total of " + gwrAdresses.Count);
            dbcomplex.BeginTransaction();
            var addedNewComplex = 0;
            var addedSingleComplex = 0;
            var addedMultipleComplexes = 0;
            var coordsAddded = 0;
            foreach (var adr in gwrAdresses) {
                var existingComplexes = complexes.Where(x => x.EGids.Contains(adr.EidgGebaeudeidentifikator_EGID)).ToList();
                BuildingComplex bc;
                if (existingComplexes.Count == 0) {
                    bc = new BuildingComplex(Guid.NewGuid().ToString(), BuildingComplex.SourceOfEntry.GWRData);
                    bc.EGids.Add(adr.EidgGebaeudeidentifikator_EGID);
                    existingComplexes.Add(bc);
                    complexes.Add(bc);
                    addedNewComplex++;
                }
                else {
                    bc = existingComplexes[0];
                    if (existingComplexes.Count == 1) {
                        addedSingleComplex++;
                    }
                    else {
                        addedMultipleComplexes++;
                    }
                }

                var addresse = adr.Strassenbezeichnung_DSTR + " " + adr.EingangsnummerGebaeude_DEINR;
                var cleanAdresse = Helpers.CleanAdressString(addresse);
                // ReSharper disable twice PossibleInvalidOperationException
                if (adr.XKoordinate_DKODX != null) {
                    bc.Coords.Add(new GeoCoord(adr.XKoordinate_DKODX.Value, adr.YKoordinate_DKODY.Value));
                    coordsAddded++;
                }

                if (!bc.CleanedAdresses.Contains(cleanAdresse)) {
                    bc.AddAdress(addresse);
                }

                dbcomplex.Save(bc);
            }

            Debug("GWRAdressen:Added new complexes: " + addedNewComplex);
            Debug("GWRAdressen:Added Addresses to single complexes: " + addedSingleComplex);
            Debug("GWRAdressen:Added Addresses to multiple complexes: " + addedMultipleComplexes);
            Debug("GWRAdressen:Coords added to:" + coordsAddded);
            var totalComplexes = complexes.Count;
            var complexesWithCoord = complexes.Count(x => x.Coords.Count > 0);
            Debug("GWRAdressen: " + complexesWithCoord + " / " + totalComplexes + " have an coordinate");
            dbcomplex.CompleteTransaction();
        }
    }
}
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data;
using Data.DataModel.Dst;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._02_Komplexes {
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class A_CreateComplexes : RunableWithBenchmark {
        public A_CreateComplexes([NotNull] ServiceRepository services)
            : base("A_CreateComplexeService", Stage.Complexes, 1, services, false)
        {
            DevelopmentStatus.Add("convert all coordinates to the same system");
            DevelopmentStatus.Add("do nearness matching for all entries");
        }

        protected override void RunActualProcess()
        {
            Log(MessageType.Info, "Starting creating complexes");
            Log(MessageType.Info, "Clearing tables", "A_CreateComplexeService");
            ClearComplexTables();
            Log(MessageType.Info, "Energiebedarfsdaten", "A_CreateComplexeService");
            P01_StartWithEnergieBedarfsdaten();
            Log(MessageType.Info, "Feurungsstaetten", "A_CreateComplexeService");
            P02_AddFeuerungsstaettenAdressDataAsComplexes();
            Log(MessageType.Info, "GWRAdressen", "A_CreateComplexeService");
            P03_AddGwrAdressen();
            Log(MessageType.Info, "Trafokreis", "A_CreateComplexeService");
            P04_AddTrafoStationData();
            Log(MessageType.Info, "Merging as needed", "A_CreateComplexeService");
            var merger = new ComplexMerger(SqlConnection, Services.MyLogger);
            merger.MergeBuildingComplexesAsNeeded();
            Log(MessageType.Info, "Merged from " + merger.BeginCount + " to " + merger.EndCount, "A_CreateComplexeService");
            Log(MessageType.Info, "Finished", "A_CreateComplexeService");
            CheckKomplexes();
        }

        public void P04_AddTrafoStationData()
        {
            var dbsrc = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbdst = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var trafokreise = dbsrc.Fetch<TrafoKreisImport>();
            var buildingComplexes = dbdst.Fetch<BuildingComplex>();
            dbdst.BeginTransaction();
            Log(MessageType.Info, "Using Trafokreis data");
            foreach (var tk in trafokreise) {
                //fehlende egids einlesen
                if (tk.U_EGID_ISE != null && tk.U_EGID_ISE != 0) {
                    var complex = buildingComplexes.FirstOrDefault(x => x.GebäudeObjectIDs.Contains(tk.U_OBJ_ID_I ?? throw new Exception("Value was null")));
                    if (complex != null && !complex.EGids.Contains(tk.U_EGID_ISE.Value)) {
                        complex.EGids.Add(tk.U_EGID_ISE.Value);
                        dbdst.Save(complex);
                    }
                }
            }

            dbdst.CompleteTransaction();
            Log(MessageType.Info, "Energiebedarfsdaten: Saved every building from Energiebedarfsdaten as first complexes");
            dbdst.CloseSharedConnection();
        }

        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        public void CheckKomplexes()
        {
            var dbdst = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var complexes = dbdst.Fetch<BuildingComplex>();
            foreach (var complex in complexes) {
                if (complex.ComplexGuid == null) {
                    throw new Exception("guid was null");
                }
            }
        }

        private void ClearComplexTables()
        {
            SqlConnection.RecreateTable<BuildingComplex>(Stage.Complexes, Constants.PresentSlice);
            SqlConnection.RecreateTable<BuildingComplexStandorte>(Stage.Complexes, Constants.PresentSlice);
        }

        private void P01_StartWithEnergieBedarfsdaten()
        {
            var dbsrc = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbdst = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var energie = dbsrc.Fetch<EnergiebedarfsdatenBern>();
            dbdst.BeginTransaction();
            Log(MessageType.Info, "Energiebedarfsdaten: Using Energebedarfsdaten as foundation for forming the complexes");
            foreach (var energiebedarfsdatenBern in energie) {
                var bc = new BuildingComplex(Guid.NewGuid().ToString(), BuildingComplex.SourceOfEntry.BernData);
                bc.EGids.Add(energiebedarfsdatenBern.egid);
                dbdst.Save(bc);
            }

            dbdst.CompleteTransaction();
            Log(MessageType.Info, "Energiebedarfsdaten: Saved every building from Energiebedarfsdaten as first complexes");
            dbdst.CloseSharedConnection();
        }

        private void P02_AddFeuerungsstaettenAdressDataAsComplexes()
        {
            Log(MessageType.Info, "Feuerungsstätten:Starting to add adressen from feurungsstätten");
            var dbComplexes = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var complexes = dbComplexes.Fetch<BuildingComplex>();
            var feuerungsStaetten = dbRaw.Fetch<FeuerungsStaette>();

            Log(MessageType.Info, "Feuerungsstätten:Found " + feuerungsStaetten.Count);
            var vegid = -1000;
            var missingCount = 1;
            foreach (var staette in feuerungsStaetten) {
                if (staette.EGID == -99) {
                    staette.EGID = vegid--;
                    Log(MessageType.Warning, "Feuringsstätte missing EGID #" + missingCount, staette);
                    missingCount++;
                }
            }

            Log(MessageType.Info, "Feuerungsstätten: Total missing Count: " + missingCount);
            dbComplexes.BeginTransaction();
            Log(MessageType.Info, "Feuerungsstätten: Trying to add the adresses from the feurungsstätten to the building complexes");
            var newComplexesCreated = 0;
            var assignedToExactlyOneComplex = 0;
            var multiplePossibleComplexes = 0;
            foreach (var fs in feuerungsStaetten) {
                Debug.Assert(fs.EGID != null, "fs.EGID != null");
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

            Log(MessageType.Info, "Feuerungsstätten: Newly created Complexes: " + newComplexesCreated);
            Log(MessageType.Info, "Feuerungsstätten: Assigned to exactly one complex: " + assignedToExactlyOneComplex);
            Log(MessageType.Info, "Feuerungsstätten: Assigned to exactly one complex out of multiple possible ones: " + multiplePossibleComplexes);
            dbComplexes.CompleteTransaction();
            dbComplexes.CloseSharedConnection();
            Log(MessageType.Info, "Feuerungsstätten:finished adding adressen from feurungsstätten");
        }


        private void P03_AddGwrAdressen()
        {
            Log(MessageType.Info, "GWRAdressen:adding adressen from gwr");
            var dbraw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice).Database;
            var dbcomplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice).Database;
            var complexes = dbcomplex.Fetch<BuildingComplex>();
            var gwrAdresses = dbraw.Fetch<GwrAdresse>();
            Log(MessageType.Info, "GWRAdressen:Found a total of " + gwrAdresses.Count);
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

            Log(MessageType.Info, "GWRAdressen:Added new complexes: " + addedNewComplex);
            Log(MessageType.Info, "GWRAdressen:Added Addresses to single complexes: " + addedSingleComplex);
            Log(MessageType.Info, "GWRAdressen:Added Addresses to multiple complexes: " + addedMultipleComplexes);
            Log(MessageType.Info, "GWRAdressen:Coords added to:" + coordsAddded);
            var totalComplexes = complexes.Count;
            var complexesWithCoord = complexes.Count(x => x.Coords.Count > 0);
            Log(MessageType.Info, "GWRAdressen: " + complexesWithCoord + " / " + totalComplexes + " have an coordinate");
            dbcomplex.CompleteTransaction();
        }
    }
}
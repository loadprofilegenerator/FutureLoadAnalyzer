using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BurgdorfStatistics.DataModel.Src;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Database;
using Common.Steps;
using Data;
using Data.DataModel.Dst;
using Data.DataModel.Src;
using JetBrains.Annotations;

namespace BurgdorfStatistics._02_Komplexes {
    // ReSharper disable once InconsistentNaming
    internal class B_KomplexeToStandorte : RunableWithBenchmark {
        public B_KomplexeToStandorte([NotNull] ServiceRepository services)
            : base(nameof(B_KomplexeToStandorte), Stage.Complexes, 2, services, true)
        {
        }

        protected override void RunActualProcess()
        {
            Log(MessageType.Info, "B_KomplexeToStandorte: Starting to add the standorte to the complexes");
            var loweredTranslations = LoadStandortTranslationsAsDictionary(SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice));
            var dbComplexes = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var komplexe = FetchCleanedComplexList(dbComplexes);
            var localnetEntries = LoadLocalnetData(dbRaw);
            var originalStandortID = CreateCleanToOriginalStandortDict(localnetEntries, out var cleanStandortToObjectID);
            Log(MessageType.Info, "B_KomplexeToStandorte: Found " + komplexe.Count + " komplexes");
            Log(MessageType.Info, "B_KomplexeToStandorte: Found " + originalStandortID.Count + " total standorte");
            //key = clean
            //value == original string
            var standorteWithoutKomplex = 0;
            var standorteWithExactlyOneKomplex = 0;
            var standorteWithTooManyKomplexes = 0;
            var komplexeByGebäudeID = 0;
            var translationsUsed = 0;
            var virtualEGIDs = -2000;
            dbComplexes.Database.BeginTransaction();
            foreach (var localnetStandort in originalStandortID) {
                var posibilitysByGebäudeID = FindComplexByGebäudeID(cleanStandortToObjectID, localnetStandort, komplexe);
                var gebäudeID = cleanStandortToObjectID[localnetStandort.Key];
                if (posibilitysByGebäudeID.Count > 0) {
                    UpdateComplexWithMissingStandort(dbComplexes, localnetStandort.Value, posibilitysByGebäudeID[0], gebäudeID);
                    komplexeByGebäudeID++;
                    if (posibilitysByGebäudeID.Count > 1) {
                        throw new Exception("Too many potential komplexe found");
                    }
                }
                else {
                    var possibilites = FindAllPossibleComplexes(komplexe, localnetStandort, ref translationsUsed, loweredTranslations);
                    if (possibilites.Count == 0) {
                        var newbc = CreateNewComplex(komplexe, ref virtualEGIDs, localnetStandort, localnetStandort.Key, gebäudeID);
                        dbComplexes.Database.Save(newbc);
                        standorteWithoutKomplex++;
                    }
                    else {
                        UpdateComplexWithMissingStandort(dbComplexes, localnetStandort.Value, possibilites[0], gebäudeID);
                        if (possibilites.Count == 1) {
                            standorteWithExactlyOneKomplex++;
                        }
                        else {
                            standorteWithTooManyKomplexes++;
                        }
                    }
                }
            }

            dbComplexes.Database.CompleteTransaction();
            Log(MessageType.Info, "B_KomplexeToStandorte: standorteWithoutKomplex:" + standorteWithoutKomplex);
            Log(MessageType.Info, "B_KomplexeToStandorte: standorteWithExactlyOneKomplex:" + standorteWithExactlyOneKomplex);
            Log(MessageType.Info, "B_KomplexeToStandorte: standorteWithTooManyKomplexes:" + standorteWithTooManyKomplexes);
            Log(MessageType.Info, "B_KomplexeToStandorte: komplexeByGebäudeID found:" + komplexeByGebäudeID);
            Log(MessageType.Info, "B_KomplexeToStandorte: total komplexe afterwards:" + komplexe.Count);
        }

        [ItemNotNull]
        [NotNull]
        private static List<BuildingComplex> FindComplexByGebäudeID([NotNull] Dictionary<string, int> cleanStandortToObjectID, KeyValuePair<string, string> localnetStandort,
                                                                    [ItemNotNull] [NotNull] List<BuildingComplex> komplexe)
        {
            var gebäudeID = cleanStandortToObjectID[localnetStandort.Key];
            var potentialComplexes = komplexe.Where(x => x.GebäudeObjectIDs.Contains(gebäudeID)).ToList();
            return potentialComplexes;
        }

        [NotNull]
        [ItemNotNull]
        private List<BuildingComplex> FetchCleanedComplexList([NotNull] MyDb db)
        {
            var komplexe = db.Database.Fetch<BuildingComplex>();
            foreach (var complex in komplexe) {
                complex.ObjektStandorte.Clear();
            }

            var komplexeToDelete = komplexe.Where(x => x.SourceOfThisEntry == BuildingComplex.SourceOfEntry.Localnetdata).ToList();
            Log(MessageType.Info, "B_KomplexeToStandorte: Deleted #:" + komplexeToDelete.Count + " komplexe from previous run");
            foreach (var complex in komplexeToDelete) {
                komplexe.Remove(complex);
                db.Database.Delete(complex);
            }

            return komplexe;
        }

        [NotNull]
        [ItemNotNull]
        private static List<BuildingComplex> FindAllPossibleComplexes([NotNull] [ItemNotNull] List<BuildingComplex> komplexe, KeyValuePair<string, string> localnetStandort, ref int translationsUsed,
                                                                      [NotNull] Dictionary<string, string> loweredTranslations)
        {
            var possibilites = new List<BuildingComplex>();
            var standortParts = localnetStandort.Value.Replace("/", ",").Split(',');

            foreach (var standortPart in standortParts) {
                var s = standortPart;
                if (s.Contains("/")) {
                    s = s.Substring(0, s.IndexOf("/", StringComparison.Ordinal)).Trim();
                }

                var cleandStandortPart = Helpers.CleanAdressString(s);
                foreach (var komplex in komplexe) {
                    if (komplex.CleanedAdresses.Contains(cleandStandortPart) && !possibilites.Contains(komplex)) {
                        possibilites.Add(komplex);
                    }
                }
            }

            if (possibilites.Count == 0) {
                var manuallytranslatedAdress = loweredTranslations[Helpers.CleanAdressString(localnetStandort.Value)];
                var cleandmanuallytranslatedAdress = Helpers.CleanAdressString(manuallytranslatedAdress);
                possibilites = komplexe.Where(x => x.CleanedAdresses.Contains(cleandmanuallytranslatedAdress)).ToList();
                if (possibilites.Count > 0) {
                    translationsUsed++;
                }
            }

            return possibilites;
        }

        [NotNull]
        [ItemNotNull]
        private List<Localnet> LoadLocalnetData([NotNull] MyDb db)
        {
            var sw = new Stopwatch();
            sw.Start();
            var localnetEntries = db.Database.Fetch<Localnet>();
            if (localnetEntries.Count == 0) {
                throw new Exception("no localnet");
            }

            sw.Stop();
            Log(MessageType.Info, "Loaded localnet data in " + sw.ElapsedMilliseconds + " ms for " + localnetEntries.Count + " entries");
            return localnetEntries;
        }

        [NotNull]
        private Dictionary<string, string> LoadStandortTranslationsAsDictionary([NotNull] MyDb db)
        {
            var adresstranslations = db.Database.Fetch<AdressTranslationEntry>();
            if (adresstranslations.Count == 0) {
                throw new Exception("no adresstranslations");
            }

            var loweredTranslations = new Dictionary<string, string>();
            foreach (var entry in adresstranslations) {
                if (entry.OriginalStandort == null) {
                    throw new Exception("Originalstandort was null");
                }

                loweredTranslations.Add(Helpers.CleanAdressString(entry.OriginalStandort), entry.TranslatedAdress);
            }

            Log(MessageType.Info, "Loaded " + loweredTranslations.Count + " translations");
            return loweredTranslations;
        }

        [NotNull]
        private static Dictionary<string, string> CreateCleanToOriginalStandortDict([NotNull] [ItemNotNull] List<Localnet> localnetEntries,
                                                                                    [NotNull] out Dictionary<string, int> cleanStandortToObjectID)
        {
            cleanStandortToObjectID = new Dictionary<string, int>();
            var originalStandortID = new Dictionary<string, string>();
            foreach (var localnetEntry in localnetEntries) {
                var standort = localnetEntry.Objektstandort;
                if (standort == null) {
                    throw new Exception("was null");
                }

                var cleanStandort = Helpers.CleanAdressString(standort);
                if (!originalStandortID.ContainsKey(cleanStandort) && !string.IsNullOrWhiteSpace(cleanStandort)) {
                    originalStandortID.Add(cleanStandort, localnetEntry.Objektstandort);
                }

                if (!cleanStandortToObjectID.ContainsKey(cleanStandort)) {
                    // ReSharper disable once PossibleInvalidOperationException
                    cleanStandortToObjectID.Add(cleanStandort, localnetEntry.ObjektIDGebäude.Value);
                }
            }

            return originalStandortID;
        }

        [NotNull]
        private static BuildingComplex CreateNewComplex([NotNull] [ItemNotNull] List<BuildingComplex> komplexe, ref int virtualEGIDs, KeyValuePair<string, string> localnetStandort,
                                                        [NotNull] string manuallytranslatedAdress, int gebäudeID)
        {
            var newbc = new BuildingComplex(Guid.NewGuid().ToString(), BuildingComplex.SourceOfEntry.Localnetdata);
            newbc.ObjektStandorte.Add(localnetStandort.Value);
            newbc.AddAdress(manuallytranslatedAdress);
            newbc.EGids.Add(virtualEGIDs--);
            newbc.GebäudeObjectIDs.Add(gebäudeID);
            komplexe.Add(newbc);
            return newbc;
        }

        private static void UpdateComplexWithMissingStandort([NotNull] MyDb db, [NotNull] string fullStandort, [NotNull] BuildingComplex bc, int gebäudeID)
        {
            if (!bc.ObjektStandorte.Contains(fullStandort)) {
                bc.ObjektStandorte.Add(fullStandort);
            }

            if (!bc.GebäudeObjectIDs.Contains(gebäudeID)) {
                bc.GebäudeObjectIDs.Add(gebäudeID);
            }

            db.Database.Save(bc);
        }
    }
}
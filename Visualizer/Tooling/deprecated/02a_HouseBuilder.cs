/*
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel;
using BurgdorfStatistics.DataModel.Dst;
using NPoco;
using NUnit.Framework;

namespace BurgdorfStatistics._02_DatasetBuilder
{
    using System;

    using Newtonsoft.Json;

    // ReSharper disable once InconsistentNaming
    internal class _02a_HouseBuilder
    {
        public _02a_HouseBuilder()
        {
            Helpers.RecreateTable<HouseEntry>();
            Helpers.RecreateTable<Customer>();

            _db = new Database("Data Source=v:\\Test.sqlite", DatabaseType.SQLite, System.Data.SQLite.SQLiteFactory.Instance);
            _localnetEntries = _db.Fetch<Localnet>();
            _localnetEntries = _localnetEntries.Where(x => !string.IsNullOrWhiteSpace(x.Objektstandort)).ToList();
            _objektStandorteZuEgid = _db.Fetch<ObjektStandortZuEgid>();

        }

        private List<ObjektStandortZuEgid> _objektStandorteZuEgid;
        private readonly List<Localnet> _localnetEntries;
        Database _db;
        [Test]
        public static void RunHousebuilder()
        {
            _02a_HouseBuilder hb = new _02a_HouseBuilder();
            hb.Run();
        }

        public void Run()
        {
            var ebd = _db.Fetch<EnergiebedarfsdatenBern>();
            Dictionary<string, List<Localnet>> localNetByStandort = MakeLocalnetByStandortDictionary(_localnetEntries);
            HashSet<Localnet> processedentries = new HashSet<Localnet>();
            foreach (var bernBedarfsEntry in ebd)
            {
                HouseEntry he = new HouseEntry();
                //he.SourceOfEntry = SourceOfEntry.BernData;
                he.EGID = bernBedarfsEntry.egid;
                he.NumberOfWohnungenEnergieBedarf = bernBedarfsEntry.ganzwhg;
                _db.Save(he);
                _db.BeginTransaction();
                //try to figure out the number of active appartments from localnet data
                CountLocalnetCustomerEntriesAndMakeCustomers(_db, _objektStandorteZuEgid, localNetByStandort, processedentries, he);
                _db.Save(he);
                _db.CompleteTransaction();
            }
            // deal with all the localnet entries that have not been assigned to a egid
            int notAssigendLocalnet = 0;
            HashSet<string> unprocessedstandorte = new HashSet<string>();
            foreach (var le in _localnetEntries)
            {
                if (!processedentries.Contains(le))
                {
                    notAssigendLocalnet++;
                    if (!unprocessedstandorte.Contains(le.Objektstandort)) {
                        unprocessedstandorte.Add(le.Objektstandort);
                    }
                }
            }

            foreach (var unprocessedStandort in unprocessedstandorte)
            {
                var objektStandortZuEgid = _objektStandorteZuEgid.First(x => x.ObjektStandort == unprocessedStandort);
                HouseEntry he = new HouseEntry();
                //he.SourceOfEntry = SourceOfEntry.BernData;
                he.EGID = objektStandortZuEgid.EGID;
                he.NumberOfWohnungenEnergieBedarf = 0;
                CountLocalnetCustomerEntriesAndMakeCustomers(_db, _objektStandorteZuEgid, localNetByStandort, processedentries, he);
                _db.Save(he);

            }

            logger.Info("Not assigned entries:" + notAssigendLocalnet + ", Standorte: " + unprocessedstandorte.Count);
            var l = unprocessedstandorte.ToList();
            l.Sort();
            foreach (var standort in l)
            {
                logger.Info(standort);
            }

        }

        private static void CountLocalnetCustomerEntriesAndMakeCustomers(Database db, List<ObjektStandortZuEgid> objektStandorteZuEgid, Dictionary<string, List<Localnet>> localNetByStandort,
                                                                         HashSet<Localnet> processedentries, HouseEntry he)
        {
            List<ObjektStandortZuEgid> objektstandortEntry = objektStandorteZuEgid.Where(x => x.EGID == he.EGID).ToList();
            if (objektstandortEntry.Count > 0)
            {
                int numberBundesabgaben = 0;
                List<string> standorteAssignedToThisHouse = new List<string>();
                foreach (var ose in objektstandortEntry)
                {
                    var subsetLocalnet = localNetByStandort[ose.ObjektStandort];
                    foreach (var localnetEntry in subsetLocalnet)
                    {
                        processedentries.Add(localnetEntry);
                    }
                    MakeCustomerEntry(db, he.EGID,he, standorteAssignedToThisHouse, ose, subsetLocalnet);
                }
                he.NumberOfElectricContracts = numberBundesabgaben;
                he.Standort = JsonConvert.SerializeObject(standorteAssignedToThisHouse, Formatting.Indented);
            }
        }

        private static Dictionary<string, List<Localnet>> MakeLocalnetByStandortDictionary(List<Localnet> localnetEntries)
        {
            Dictionary<string, List<Localnet>> localNetByStandort = new Dictionary<string, List<Localnet>>();
            foreach (Localnet entry in localnetEntries)
            {
                if(string.IsNullOrWhiteSpace(entry.Objektstandort )) {
                    continue;
                }

                if (!localNetByStandort.ContainsKey(entry.Objektstandort))
                {
                    localNetByStandort.Add(entry.Objektstandort, new List<Localnet>());
                }
                localNetByStandort[entry.Objektstandort].Add(entry);
            }

            return localNetByStandort;
        }

        private static void MakeCustomerEntry(Database db, long egid, HouseEntry he, List<string> standorte, ObjektStandortZuEgid ose, List<Localnet> subsetLocalnet)
        {
            Customer cu = new Customer();
            cu.EGID = egid;
            cu.Standort = ose.ObjektStandort;
            db.Save(cu);
            standorte.Add(ose.ObjektStandort);
            //each bundesabgabe counts as one entry
            bool foundBundesabgabe = false;
            foreach (Localnet le in subsetLocalnet)
            {
                if (le.Verrechnungstyp == "Bundesabgabe zum Schutz der Gewässer und Fische") {
                    he.NumberOfElectricContracts++;
                    foundBundesabgabe = true;
                }

                if (le.VerrechnungstypArt == "Erdgas") {
                    he.HasAtLeastOneGasHeatingContract = true;
                }
            }

            cu.CustomerPaysBundesabgabe = foundBundesabgabe;
        }
    }
}
*/


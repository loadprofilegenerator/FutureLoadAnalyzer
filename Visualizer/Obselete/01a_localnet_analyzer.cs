/*using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel;
using NUnit.Framework;

namespace BurgdorfStatistics._01_Analysis
{
    // ReSharper disable once InconsistentNaming
    internal class _01_localnet_analyzer
    {
        [Test]
        public void CompareGebäudeIDvsObjektStandort()
        {
            var db = Helpers.Getdatabase();
            var localnet = db.Fetch<Localnet>();
            Dictionary<int, List<string>> gebäudeToID = new Dictionary<int, List<string>>();
            foreach (Localnet entry in localnet) {
                if (entry.ObjektIDGebäude == null) {
                    throw new Exception("ObjektIDGebäude was null.");
                }

                int id = (int)entry.ObjektIDGebäude;
                if (!gebäudeToID.ContainsKey(id))
                {
                    gebäudeToID.Add(id, new List<string>());
                }

                var list = gebäudeToID[id];
                if (!list.Contains(entry.Objektstandort))
                {
                    list.Add(entry.Objektstandort);
                }
            }

            foreach (var p in gebäudeToID)
            {
                if (p.Value.Count != 1) {
                    logger.Info((p.Key + " " + p.Value.Count);
                }

                foreach (string s in p.Value)
                {
                    logger.Info(("    " + s);
                }
            }

        }

        [Test]
        public void MakeVerbrauchProObjektstandort()
        {
            Helpers.RecreateTable<ObjektStandortEnergie>();
            var db = Helpers.Getdatabase();
            var localnetList = db.Fetch<Localnet>();
            Dictionary<string, List<Localnet>> assignedEntries = new Dictionary<string, List<Localnet>>();
            foreach (var le in localnetList)
            {
                string key = ObjektStandortEnergie.MakeKey(le);
                if (!assignedEntries.ContainsKey(key) && le.IsEnergyValue())
                {
                    assignedEntries.Add(key, new List<Localnet>());
                }

                if (le.BasisVerbrauch != null && le.IsEnergyValue()) {
                    assignedEntries[key].Add(le);
                }
            }

            db.BeginTransaction();
            int maxAbrechnungen = 0;
            foreach (KeyValuePair<string, List<Localnet>> assignedEntry in assignedEntries)
            {

                int abrechnungen = assignedEntry.Value.Count;


                if (abrechnungen > maxAbrechnungen)
                {
                    Log(assignedEntry.Key);
                    foreach (Localnet le in assignedEntry.Value)
                    {
                        Log("   " + le.Verrechnungstyp + " " + le.BasisVerbrauch);
                    }

                    maxAbrechnungen = abrechnungen;
                }
                var fe = assignedEntry.Value[0];
                ObjektStandortEnergie ose = new ObjektStandortEnergie(fe);
                for (int i = 1; i < assignedEntry.Value.Count; i++)
                {
                    var fe1 = assignedEntry.Value[i];
                    if(fe1.BasisVerbrauch == null) {
                        throw new Exception("");
                    }

                    ose.Verbrauch += fe1.BasisVerbrauch.Value;
                    ose.NumberOfEntries++;
                }
                db.Save(ose);
            }

            db.CompleteTransaction();
            Log("maximale Abrechnungen pro key" + maxAbrechnungen);
        }

        [Test]
        public void MakeVerbrauchSum()
        {
            var db = Helpers.Getdatabase();
            var energie = db.Fetch<ObjektStandortEnergie>();
            List<string> energietraeger = energie.Select(x => x.VerrechnungsTypArt_Energieträger).Distinct().ToList();
            var quartals = energie.Select(x => x.Quartal).Distinct().ToList();
            foreach (string s in energietraeger)
            {
                foreach (var quartal in quartals)
                {


                    double sum = energie.Where(x => x.VerrechnungsTypArt_Energieträger == s && x.Quartal == quartal ).Select(y => y.Verbrauch)
                        .Sum();
                    Log(s + ": " + sum.ToString("N1"));
                }
            }
        }
        [Test]
        public void FindVerbrauchAllObjektsWithoutEGID()
        {
            var db = Helpers.Getdatabase();
            var energie = db.Fetch<ObjektStandortEnergie>();
            var standorteWithoutEGids = db.Fetch<StandorteWithoutEGID>();
            var energietraeger = energie.Select(x => x.VerrechnungsTypArt_Energieträger).Distinct().ToList();
            //var quartals = energie.Select(x => x.Quartal).Distinct().ToList();
            var standorte = standorteWithoutEGids.Select(x => x.Standort).Distinct().ToList();
            foreach (string s in energietraeger)
            {
                logger.Info((s);
                    var list = energie.Where(x => x.VerrechnungsTypArt_Energieträger == s && standorte.Contains(x.ObjektStandort) ).OrderByDescending(x=> x.Verbrauch).ToList();
                        //Log(energy.ObjektStandort + " " + energy.Quartal +  ": " +  energy.Verbrauch.ToString("N1"));
                    Log(s + ": " +list.Select(x=> x.Verbrauch).Sum().ToString("N1"));
            }
        }

        [Test]
        public void CalculateVerbrauchByVerbrauchsKategorie()
        {
            var db = Helpers.Getdatabase();
            var energie = db.Fetch<ObjektStandortEnergie>();
            var standorteWithoutEGids = db.Fetch<StandorteWithoutEGID>();
            var energietraeger = energie.Select(x => x.VerrechnungsTypArt_Energieträger).Distinct().ToList();
            //var quartals = energie.Select(x => x.Quartal).Distinct().ToList();
            var standorte = standorteWithoutEGids.Select(x => x.Standort).Distinct().ToList();
            foreach (string s in energietraeger)
            {
                Log(s);
                var list = energie.Where(x => x.VerrechnungsTypArt_Energieträger == s && standorte.Contains(x.ObjektStandort)).OrderByDescending(x => x.Verbrauch).ToList();
                //Log(energy.ObjektStandort + " " + energy.Quartal +  ": " +  energy.Verbrauch.ToString("N1"));
                Log(s + ": " + list.Select(x => x.Verbrauch).Sum().ToString("N1"));
            }
        }
    }
}
*/


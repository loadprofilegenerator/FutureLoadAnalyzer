using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NPoco;
using SQLite;

namespace Data.DataModel.Dst {
    [TableName(nameof(BuildingComplex))]
    [NPoco.PrimaryKey(nameof(ComplexID))]
    [Table(nameof(BuildingComplex))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class BuildingComplex {
        public enum SourceOfEntry {
            None,
            BernData,
            Localnetdata,
            GWRData,
            Feuerungsstaetten
        }

        public BuildingComplex([JetBrains.Annotations.NotNull] string guid, SourceOfEntry source)
        {
            ComplexGuid = guid ?? throw new Exception("gUID WAS NULL");
            ComplexName = "";
            SourceOfThisEntry = source;
        }

        // ReSharper disable once NotNullMemberIsNotInitialized
        //json only
        public BuildingComplex()
        {
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> Adresses { get; set; } = new List<string>();

        [JetBrains.Annotations.NotNull]
        public string AdressesAsJson {
            get => JsonConvert.SerializeObject(Adresses, Formatting.Indented);
            set {
                Adresses = JsonConvert.DeserializeObject<List<string>>(value);
                Adresses.Sort();
                UpdateCleanedAdresses();
            }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> CleanedAdresses { get; set; } = new List<string>();

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<int> GebäudeObjectIDs { get; set; } = new List<int>();

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<GeoCoord> Coords { get; set; } = new List<GeoCoord>();

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<GeoCoord> LocalnetCoords { get; set; } = new List<GeoCoord>();

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> CleanedStandorte { get; set; } = new List<string>();

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ComplexID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ComplexName { get; set; }

        [JetBrains.Annotations.NotNull]
        public string ComplexGuid { get; set; }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        public List<long> EGids { get; set; } = new List<long>();

        [JetBrains.Annotations.NotNull]
        public string EGIDsAsJson {
            get => JsonConvert.SerializeObject(EGids);
            set {
                EGids = JsonConvert.DeserializeObject<List<long>>(value);
                EGids.Sort();
            }
        }

        [JetBrains.Annotations.NotNull]
        public string GeoCoordsAsJson {
            get => JsonConvert.SerializeObject(Coords);
            set => Coords = JsonConvert.DeserializeObject<List<GeoCoord>>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string LocalnetGeoCoordsAsJson {
            get => JsonConvert.SerializeObject(LocalnetCoords);
            set => LocalnetCoords = JsonConvert.DeserializeObject<List<GeoCoord>>(value);
        }

        [JetBrains.Annotations.NotNull]
        public string GebäudeObjectIDsAsJson {
            get => JsonConvert.SerializeObject(GebäudeObjectIDs);
            set => GebäudeObjectIDs = JsonConvert.DeserializeObject<List<int>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> ObjektStandorte { get; set; } = new List<string>();

        public SourceOfEntry SourceOfThisEntry { get; set; }

        [JetBrains.Annotations.NotNull]
        public string StandorteAsJson {
            get => JsonConvert.SerializeObject(ObjektStandorte, Formatting.Indented);
            set {
                ObjektStandorte = JsonConvert.DeserializeObject<List<string>>(value);
                UpdateCleanedStandorte();
            }
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> TrafoKreise { get; set; } = new List<string>();

        [JetBrains.Annotations.NotNull]
        public string TrafoKreiseAsJson {
            get => JsonConvert.SerializeObject(TrafoKreise, Formatting.Indented);
            set => TrafoKreise = JsonConvert.DeserializeObject<List<string>>(value);
        }

        [NPoco.Ignore]
        [SQLite.Ignore]
        [JetBrains.Annotations.NotNull]
        [ItemNotNull]
        public List<string> ErzeugerIDs { get; set; } = new List<string>();

        [JetBrains.Annotations.NotNull]
        public string ErzeugerIDsAsJson {
            get => JsonConvert.SerializeObject(ErzeugerIDs, Formatting.Indented);
            set => ErzeugerIDs = JsonConvert.DeserializeObject<List<string>>(value);
        }

        public void AddAdress([JetBrains.Annotations.NotNull] string s)
        {
            /*if (s == "Wangelenrain 27, 3400 Burgdorf") {
               Log(Info, "hi");
            }*/

            var cleanAdress = Helpers.CleanAdressString(s);
            if (!CleanedAdresses.Contains(cleanAdress)) {
                while (s.Contains("  ")) {
                    s = s.Replace("  ", " ");
                }

                Adresses.Add(s);
                Adresses.Sort();
                UpdateCleanedAdresses();
            }
        }

        [JetBrains.Annotations.NotNull]
        public override string ToString() => ComplexID + " " + EGIDsAsJson + " " + AdressesAsJson;

        private void UpdateCleanedAdresses()
        {
            CleanedAdresses = Adresses.Select(x => Helpers.CleanAdressString(x)).ToList();
        }

        private void UpdateCleanedStandorte()
        {
            CleanedStandorte = ObjektStandorte.Select(x => Helpers.CleanAdressString(x)).ToList();
        }

        public void AddCoord([JetBrains.Annotations.NotNull] GeoCoord c2Coord)
        {
            Coords.Add(c2Coord);
        }

        public void AddLocalCoord([JetBrains.Annotations.NotNull] GeoCoord c2Coord)
        {
            LocalnetCoords.Add(c2Coord);
        }

        public void AddGebäudeID(int c2ID)
        {
            GebäudeObjectIDs.Add(c2ID);
        }
    }
}
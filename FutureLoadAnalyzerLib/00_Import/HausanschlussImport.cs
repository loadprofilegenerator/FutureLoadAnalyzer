using System;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    public class HausanschlussImport {

        [Obsolete("for json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public HausanschlussImport()
        {
        }

        public HausanschlussImport([NotNull] HausanschlussImport hai, [CanBeNull] string standort)
        {
            ObjectID = hai.ObjectID;
            Egid = hai.Egid;
            Isn = hai.Isn;
            Lon = hai.Lon;
            Lat = hai.Lat;
            Trafokreis = hai.Trafokreis;
            Standort = standort;
            Adress = hai.Adress;
        }

        public HausanschlussImport([NotNull] string trafokreis, [NotNull] string objectID, long egid, int isn,
                                   double lon, double lat, [NotNull] string adress)
        {
            ObjectID = objectID;
            Egid = egid;
            Isn = isn;
            Lon = lon;
            Lat = lat;
            Trafokreis = trafokreis;
            Adress = adress;
        }

        public int ID { get; set; }

        [NotNull]
        public string ObjectID { get; set; }
        public long Egid { get; set; }
        public int Isn { get; set; }
        public double Lon { get; set; }
        public double Lat { get; set; }
        [NotNull]
        public string Trafokreis { get; set; }
        [CanBeNull]
        public string Standort { get; set; }
        [CanBeNull]
        public string Adress { get; set; }
    }
    public class HausanschlussImportSupplement {
        [NotNull]
        public string TargetComplexName { get; set; }

        [NotNull]
        public string TargetStandort { get; set; }
        public int TargetIsn { get; set; }
        [CanBeNull]
        public string HaFilename { get; set; }
        [NotNull]
        public string HaObjectid { get; set; }
        public int HaEgid { get; set; }
        public int HaIsn { get; set; }
        public double HaLon { get; set; }
        public double HaLat { get; set; }
        [NotNull]
        public string HaAdress { get; set; }

        [Obsolete("for json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public HausanschlussImportSupplement()
        {
        }

        public HausanschlussImportSupplement([NotNull] string targetComplexName, [NotNull] string targetStandort, int targetIsn,
                                   [CanBeNull] string haFilename, [NotNull] string haObjectid,
                                   int haEgid, int haIsn,
                                   double haLon, double haLat,[NotNull] string haAdress
            )
        {
            TargetComplexName = targetComplexName;
            TargetStandort = targetStandort;
            TargetIsn = targetIsn;
            HaFilename = haFilename;
            HaObjectid = haObjectid;
            HaEgid = haEgid;
            HaIsn = haIsn;
            HaLon = haLon;
            HaLat = haLat;
            HaAdress = haAdress;
        }

        public int ID { get; set; }

    }
}
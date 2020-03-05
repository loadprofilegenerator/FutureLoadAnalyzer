using System;
using JetBrains.Annotations;

namespace BurgdorfStatistics._00_Import {
    public class HausanschlussImport {

        [Obsolete("for json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public HausanschlussImport()
        {
        }

        public HausanschlussImport([NotNull] string trafokreis, [NotNull] string objectID, long egid, int isn,
                                   double lon, double lat,   [NotNull] string adress)
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
        [NotNull]
        public string Adress { get; set; }
    }
}
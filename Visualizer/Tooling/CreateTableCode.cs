using System;
using System.IO;
using BurgdorfStatistics.DataModel.Src;
using SQLite;
using Xunit;

namespace BurgdorfStatistics.Tooling {
    public class CreateTableCode {
        private void Writeclass([JetBrains.Annotations.NotNull] string classname, [JetBrains.Annotations.NotNull] string fields)
        {
            const string path = @"V:\Dropbox\BurgdorfStatistics\BurgdorfStatistics\DataModel";
            var dstfile = Path.Combine(path, classname + ".cs");
            using (var sw = new StreamWriter(dstfile)) {
                sw.WriteLine("using System;");
                sw.WriteLine("// ReSharper disable All");
                sw.WriteLine("namespace BurgdorfStatistics {");
                sw.WriteLine("public class  " + classname + "{");
                sw.WriteLine(fields);
                sw.WriteLine("}}");
                sw.Close();
            }
        }

        [Fact]
        public void MakeStrTable()
        {
            var s = "";
            s += CleanText("Eidg. Gebäudeidentifikator", "EGID", "int");
            s += CleanText("Eidg. Eingangsidentifikator", "EDID", "int");
            s += CleanText("Erhebungsstelle Baustatistik", "DESTNR", "int");
            s += CleanText("Bauprojekt-Id Liefersystem", "DBABID", "int");
            s += CleanText("Gebäude-Id Liefersystem", "DBAGID", "int");
            s += CleanText("Eingangs-Id Liefersystem", "DBADID", "int");
            s += CleanText("BFS-Gemeindenummer", "GGDENR", "int");
            s += CleanText("Gebäudeeingangstatus", "DSTAT", "int");
            s += CleanText("Strassenbezeichnung", "DSTR", "str");
            s += CleanText("Eingangsnummer Gebäude", "DEINR", "int");
            s += CleanText("Amtliche Strassennummer", "DSTRANR", "int");
            s += CleanText("Eidg. Strassenidentifikator", "DSTRID", "int");
            s += CleanText("Amtlicher Adresscode", "DADRC", "int");
            s += CleanText("Postleitzahl", "DPLZ4", "int");
            s += CleanText("PLZ-Zusatzziffer", "DPLZZ", "int");
            s += CleanText("E-Koordinate", "DKODE", "int");
            s += CleanText("N-Koordinate", "DKODN", "int");
            s += CleanText("X-Koordinate", "DKODX", "int");
            s += CleanText("Y-Koordinate", "DKODY", "int");
            s += CleanText("Plausibilitätsstatus", "DPLAUS", "int");
            s += CleanText("Datum der letzten Änderung", "DMUTDAT", "datetime");
            s += CleanText("Datum des Exports", "DEXPDAT", "datetime");
            Writeclass("GWRAdressen", s);
        }

        [Fact]
        public void MakeDataTable()
        {
            var s = "";
            s += CleanText("Eidg. Gebäudeidentifikator", "EGID", "int");
            s += CleanText("Eidg. Bauprojektidentifikator", "EPROID", "int");
            s += CleanText("Erhebungsstelle Baustatistik", "GESTNR", "int");
            s += CleanText("Bauprojekt-Id Liefersystem", "GBABID", "int");
            s += CleanText("Gebäude-ID Liefersystem", "GBAGID", "int");
            s += CleanText("BFS-Gemeindenummer", "GDENR", "int");
            s += CleanText("Eidg. Grundstücksidentifikator", "GEGRID", "str");
            s += CleanText("Grundbuchkreisnummer", "GGBKR", "int");
            s += CleanText("Parzellennummer", "GPARZ", "str");
            s += CleanText("Amtliche Gebäudenummer", "GEBNR", "int");
            s += CleanText("Name des Gebäudes", "GBEZ", "int");
            s += CleanText("Anzahl Gebäudeeingänge", "GANZDOM", "int");
            s += CleanText("E-Koordinate", "GKODE", "int");
            s += CleanText("N-Koordinate", "GKODN", "int");
            s += CleanText("X-Koordinate", "GKODX", "int");
            s += CleanText("Y-Koordinate", "GKODY", "int");
            s += CleanText("Koordinatenherkunft", "GKSCE", "int");
            s += CleanText("Lokalcode 1", "GLOC1", "int");
            s += CleanText("Lokalcode 2", "GLOC2", "int");
            s += CleanText("Lokalcode 3", "GLOC3", "int");
            s += CleanText("Lokalcode 4", "GLOC4", "int");
            s += CleanText("Gebäudestatus", "GSTAT", "int");
            s += CleanText("Gebäudekategorie", "GKAT", "int");
            s += CleanText("Gebäudeklasse", "GKLAS", "int");
            s += CleanText("Baujahr", "GBAUJ", "int");
            s += CleanText("Bauperiode", "GBAUP", "int");
            s += CleanText("Renovationsjahr", "GRENJ", "int");
            s += CleanText("Renovationsperiode", "GRENP", "int");
            s += CleanText("Abbruchjahr", "GABBJ", "int");
            s += CleanText("Gebäudefläche", "GAREA", "int");
            s += CleanText("Anzahl Geschosse", "GASTW", "int");
            s += CleanText("Anzahl separate Wohnräume", "GAZZI", "int");
            s += CleanText("Anzahl Wohnungen", "GANZWHG", "int");
            s += CleanText("Heizungsart", "GHEIZ", "int");
            s += CleanText("Energieträger der Heizung", "GENHZ", "int");
            s += CleanText("Warmwasserversorgung", "GWWV", "int");
            s += CleanText("Energieträger für Warmwasser", "GENWW", "int");
            s += CleanText("Anzahl Eingangsrecords", "GADOM", "int");
            s += CleanText("Anzahl Wohnungsrecords", "GAWHG", "int");
            s += CleanText("Plausibilitätsstatus der Koordinaten", "GKPLAUS", "int");
            s += CleanText("Status Wohnungsbestandes", "GWHGSTD", "int");
            s += CleanText("Verifikation Wohnungsbestand", "GWHGVER", "int");
            s += CleanText("Plausibilitätsstatus Gebäude", "GPLAUS", "int");
            s += CleanText("Baumonat", "GBAUM", "int");
            s += CleanText("Renovationsmonat", "GRENM", "int");
            s += CleanText("Abbruchmonat", "GABBM", "int");
            s += CleanText("Datum der letzten Änderung", "GMUTDAT", "int");
            s += CleanText("Datum des Exports", "GEXPDAT", "int");
            Writeclass("GWRData", s);
        }

        [Fact]
        public void CreateTable()
        {
            using (var db = new SQLiteConnection("v:\\test.sqlite")) {
                db.DropTable<GwrAdresse>();
                db.CreateTable<GwrAdresse>();
                db.CreateTable<GwrData>();
            }
        }

        [JetBrains.Annotations.NotNull]
        public string CleanText([JetBrains.Annotations.NotNull] string name1, [JetBrains.Annotations.NotNull] string name2, [JetBrains.Annotations.NotNull] string datatype)
        {
            name1 = name1.Replace(" ", "").Replace("-", "").Replace("ä", "ae").Replace(".", "").Replace("Ä", "Ae").Replace("_", "").Replace("ü", "ue");
            string type;
            switch (datatype) {
                case "int":
                    type = "public int";
                    break;
                case "str":
                    type = "public string";
                    break;
                case "datetime":
                    type = "public DateTime";
                    break;
                default:
                    throw new Exception("unknown type: " + datatype);
            }

            return type + " " + name1 + "_" + name2 + " {get;set;}\r\n";
        }
    }
}
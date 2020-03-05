using System;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._00_Import {
    public class WasserkraftwerkImport {
        [Obsolete("for json only")]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public WasserkraftwerkImport()
        {
        }

        public WasserkraftwerkImport([NotNull] string bezeichnung,
                                     [NotNull] string anlagennummer,
                                     [NotNull] string adresse,
                                     [NotNull] string status,
                                     [CanBeNull] string inbetriebnahme,
                                     double nennleistung,
                                     [NotNull] string standort,
                                     [NotNull] string complexName,
                                     [NotNull] string lastProfil)
        {
            Bezeichnung = bezeichnung;
            Anlagennummer = anlagennummer;
            Adresse = adresse;
            Status = status;
            Inbetriebnahme = inbetriebnahme;
            Nennleistung = nennleistung;
            Standort = standort;
            ComplexName = complexName;
            LastProfil = lastProfil;
        }

        [NotNull]
        public string Adresse { get; set; }

        [NotNull]
        public string Anlagennummer { get; set; }

        [NotNull]
        public string Bezeichnung { get; set; }

        [NotNull]
        public string ComplexName { get; set; }

        public int ID { get; set; }

        [CanBeNull]
        public string Inbetriebnahme { get; set; }

        [NotNull]
        public string LastProfil { get; set; }

        public double Nennleistung { get; set; }

        [NotNull]
        public string Standort { get; set; }

        [NotNull]
        public string Status { get; set; }
    }
}
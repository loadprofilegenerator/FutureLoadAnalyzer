using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.Src {
    [TableName(nameof(LocalnetTrafostation))]
    [NPoco.PrimaryKey(nameof(ID))]
    [Table(nameof(LocalnetTrafostation))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class LocalnetTrafostation {
        [Obsolete("only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public LocalnetTrafostation()
        {
        }

        public LocalnetTrafostation([JetBrains.Annotations.NotNull] string bezeichnung,
                                    [JetBrains.Annotations.NotNull] string seriennummer,
                                    [JetBrains.Annotations.NotNull] string hersteller,
                                    [CanBeNull] string art, [JetBrains.Annotations.NotNull] string status,
                                    [JetBrains.Annotations.NotNull] string eingebautInLagerort,
                                    [JetBrains.Annotations.NotNull] string einbauort, [JetBrains.Annotations.NotNull] string adresse,
                                    [CanBeNull] string vorlage, [JetBrains.Annotations.NotNull] string komponentenart,
                                    [JetBrains.Annotations.NotNull] string leistungKVa,
                                    [CanBeNull] string primärnennstromA,
                                    [CanBeNull] string sekundärnennstromA,
                                    [CanBeNull] string baujahr,
                                    [CanBeNull] string sekundärstromA, [JetBrains.Annotations.NotNull] string schaltgruppe,
                                    [JetBrains.Annotations.NotNull] string kurzschlussspannung, [JetBrains.Annotations.NotNull] string eisenverlusteW,
                                    [JetBrains.Annotations.NotNull] string kupferverlusteW, [JetBrains.Annotations.NotNull] string ikSekKA, [JetBrains.Annotations.NotNull] string betriebsstatus)
        {
            Bezeichnung = bezeichnung;
            Seriennummer = seriennummer;
            Hersteller = hersteller;
            Art = art;
            Status = status;
            Eingebaut_in_Lagerort = eingebautInLagerort;
            Einbauort = einbauort;
            Adresse = adresse;
            Vorlage = vorlage;
            Komponentenart = komponentenart;
            Leistung_kVA = leistungKVa;
            Primärnennstrom_A = primärnennstromA;
            Sekundärnennstrom_A = sekundärnennstromA;
            Baujahr = baujahr;
            Sekundärstrom_A = sekundärstromA;
            Schaltgruppe = schaltgruppe;
            Kurzschlussspannung = kurzschlussspannung;
            Eisenverluste_W = eisenverlusteW;
            Kupferverluste_W = kupferverlusteW;
            Ik_sek_kA = ikSekKA;
            Betriebsstatus = betriebsstatus;
        }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Bezeichnung { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Seriennummer { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Hersteller { get; set; }

        [CanBeNull]
        public string Art { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Status { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Eingebaut_in_Lagerort { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Einbauort { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Adresse { get; set; }

        [CanBeNull]
        public string Vorlage { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Komponentenart { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Leistung_kVA { get; set; }
        [CanBeNull]
        public string Primärnennstrom_A { get; set; }
        [CanBeNull]
        public string Sekundärnennstrom_A { get; set; }
        [CanBeNull]
        public string Baujahr { get; set; }
        [CanBeNull]
        public string Sekundärstrom_A { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Schaltgruppe { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Kurzschlussspannung { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Eisenverluste_W { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Kupferverluste_W { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Ik_sek_kA { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Betriebsstatus { get; set; }
    }
}
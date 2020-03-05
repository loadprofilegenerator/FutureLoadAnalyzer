//first attempt, not good
/*using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel;
using BurgdorfStatistics.DataModel.Dst;
using BurgdorfStatistics.Logger;
using BurgdorfStatistics._00_Import;
using JetBrains.Annotations;

namespace BurgdorfStatistics._03_KomplexEnergy {
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once InconsistentNaming
    internal class B_AssignLocalnetDataToComplexes : RunableWithBenchmark {
        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<ComplexLocalnetDataAnalysis>(Stage.ComplexEnergyData);
            var dbComplexes = SqlConnection.GetDatabaseConnection(Stage.Complexes);
            var dbEnergy = SqlConnection.GetDatabaseConnection(Stage.ComplexEnergyData);
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw);
            var complexes = dbComplexes.Fetch<BuildingComplex>();
            var localnetEntries = dbRaw.Fetch<Localnet>();
            List<ComplexLocalnetDataAnalysis> clds = new List<ComplexLocalnetDataAnalysis>();
            dbEnergy.BeginTransaction();
            Dictionary<string, BuildingComplex> complexDict = new Dictionary<string, BuildingComplex>();
            foreach (BuildingComplex complex in complexes)
            {
                foreach (string standort in complex.CleanedStandorte)
                {
                    complexDict.Add(standort, complex);
                }
            }

            int newcomplexdataCreated = 0;
            int complexDataMerged = 0;
            foreach (Localnet localnet in localnetEntries)
            {
                if (string.IsNullOrWhiteSpace(localnet.Objektstandort))
                {
                    continue;
                }
                string cleanedStandort = Helpers.CleanAdressString(localnet.Objektstandort);
                var k = complexDict[cleanedStandort];
                ComplexLocalnetDataAnalysis cld = clds.FirstOrDefault(x => x.ComplexName == k.ComplexName);
                if (cld == null)
                {
                    cld = new ComplexLocalnetDataAnalysis
                    {
                        ComplexName = k.ComplexName
                    };
                    newcomplexdataCreated++;
                    clds.Add(cld);
                }
                else {
                    complexDataMerged++;
                }
                cld.LocalnetEntries.Add(localnet);
                cld.EntryCount++;
                ProcessVerrechnungstyp(localnet, cld);
            }

            foreach (ComplexLocalnetDataAnalysis cld in clds)
            {
                cld.LocalnetEntries.Sort((x, y) => x.Termin.CompareTo(y.Termin));
                dbEnergy.Save(cld);
            }
            Log(MessageType.Info,"newly created complex data entries: " + newcomplexdataCreated+ "/"+ localnetEntries.Count);
            Log(MessageType.Info, "merged complex data entries: " + complexDataMerged + "/" + localnetEntries.Count);
            dbEnergy.CompleteTransaction();
        }

        private static void ProcessVerrechnungstyp([NotNull] Localnet localnet, [NotNull] ComplexLocalnetDataAnalysis cld)
        {
            switch (localnet.Verrechnungstyp)
            {
                case "Gutschrift Erdgas":
                    cld.GutschriftErdgas++;
                    break;
                case "Kosten Erdgasbezug":
                    cld.KostenErdgasbezug++;
                    break;
                case "Erdgasverbrauch":
                    cld.Erdgasverbrauch++;
                    break;
                case "CO2-Abgabe":
                    cld.CO2Abgabe++;
                    break;
                case "Grundpreis Erdgas":
                    cld.GrundpreisErdgas++;
                    break;
                case "Grundgebühr Erdgas":
                    cld.GrundgebührErdgas++;
                    break;
                case "Leistungsspitze":
                    cld.Leistungsspitze++;
                    break;
                case "Arbeitspreis":
                    cld.Arbeitspreis++;
                    break;
                case "Grundpreis Wärme (kW/Mt)":
                    cld.GrundpreisWärmekWMt++;
                    break;
                case "Grundpreis Wärme (Zähler/Mt)":
                    cld.GrundpreisWärmeZählerMt++;
                    break;
                case "Grundpreis 1 (kW/Mt)":
                    cld.Grundpreis1kWMt++;
                    break;
                case "Grundpreis 2 (kW/Mt)":
                    cld.Grundpreis2kWMt++;
                    break;
                case "Energie Tagesstrom (HT)":
                    cld.EnergieTagesstromHT++;
                    break;
                case "Energie Nachtstrom (NT)":
                    cld.EnergieNachtstromNT++;
                    break;
                case "Gutschrift Strom Energie":
                    cld.GutschriftStromEnergie++;
                    break;
                case "Nachbelastung Strom Energie":
                    cld.NachbelastungStromEnergie++;
                    break;
                case "Rücklieferung Tagesstrom HT":
                    cld.RücklieferungTagesstromHT++;
                    break;
                case "Rücklieferung Nachtstrom NT":
                    cld.RücklieferungNachtstromNT++;
                    break;
                case "EVG Tagesstrom (HT)":
                    cld.EVGTagesstromHT++;
                    break;
                case "EVG Nachtstrom (NT)":
                    cld.EVGNachtstromNT++;
                    break;
                case "Leistung":
                    cld.Leistung++;
                    break;
                case "Blindenergie (HT)":
                    cld.BlindenergieHT++;
                    break;
                case "Blindenergie (NT)":
                    cld.BlindenergieNT++;
                    break;
                case "Grundpreis Strom":
                    cld.GrundpreisStrom++;
                    break;
                case "Netz Tagesstrom (HT)":
                    cld.NetzTagesstromHT++;
                    break;
                case "Netz Nachtstrom (NT)":
                    cld.NetzNachtstromNT++;
                    break;
                case "Messung und Abrechnung":
                    cld.MessungundAbrechnung++;
                    break;
                case "Systemdienstleistungen Swissgrid (SDL)":
                    if (localnet.ObjektIDGebäude > 0)
                    {
                        cld.SystemdienstleistungenSwissgridSDL++;
                        if(localnet.TerminString == null) {
                            throw new Exception("was null");
                        }

                        if (!cld.SDLDateCounts.ContainsKey(localnet.TerminString))
                        {
                            cld.SDLDateCounts.Add(localnet.TerminString, 0);
                        }

                        cld.SDLDateCounts[localnet.TerminString]++;
                    }

                    break;
                case "Kostendeckende Einspeisevergütung KEV":
                    cld.KostendeckendeEinspeisevergütungKEV++;
                    break;
                case "Abgabe an Gemeinde":
                    cld.AbgabeanGemeinde++;
                    if (localnet.ObjektIDGebäude > 0)
                    {
                        if(localnet.TerminString==null) {
                            throw new Exception("was null");
                        }

                        if (!cld.GemeindeCounts.ContainsKey(localnet.TerminString))
                        {
                            cld.GemeindeCounts.Add(localnet.TerminString, 0);
                            cld.GemeindeCounts2.Add(localnet.TerminString, 0);
                        }

                        if (!cld.GemeindeObjektStandoirte.ContainsKey(localnet.TerminString))
                        {
                            cld.GemeindeObjektStandoirte.Add(localnet.TerminString, new HashSet<string>());
                        }

                        if (!cld.GemeindeObjektStandoirte[localnet.TerminString].Contains(localnet.Objektstandort))
                        {
                            cld.GemeindeObjektStandoirte[localnet.TerminString].Add(localnet.Objektstandort);
                            cld.GemeindeCounts2[localnet.TerminString]++;
                        }
                        cld.GemeindeCounts[localnet.TerminString]++;
                    }
                    break;
                case "Gutschrift Strom Netznutzung":
                    cld.GutschriftStromNetznutzung++;
                    break;
                case "Bundesabgabe zum Schutz der Gewässer und Fische":
                    cld.BundesabgabezumSchutzderGewsserundFische++;
                    break;
                case "Netz Rücklieferung Tagesstrom HT":
                    cld.NetzRücklieferungTagesstromHT++;
                    break;
                case "Netz Rücklieferung Nachtstrom NT":
                    cld.NetzRücklieferungNachtstromNT++;
                    break;
                case "Messung und Abrechung Netzübergabestelle":
                    cld.MessungundAbrechungNetzübergabestelle++;
                    break;
                case "EVG Leistung":
                    cld.EVGLeistung++;
                    break;
                case "EVG Blindenergie (HT)":
                    cld.EVGBlindenergieHT++;
                    break;
                case "EVG Blindenergie (NT)":
                    cld.EVGBlindenergieNT++;
                    break;
                default:
                    throw new Exception("unknown verrechnungstyp");
            }
        }

        public B_AssignLocalnetDataToComplexes([NotNull] MySqlConnection sqlConnection, [NotNull] Logger.Logger logger) : base("B_LocalnetEntryCounter", sqlConnection,logger,Stage.ComplexEnergyData)
        {
        }
    }
}*/


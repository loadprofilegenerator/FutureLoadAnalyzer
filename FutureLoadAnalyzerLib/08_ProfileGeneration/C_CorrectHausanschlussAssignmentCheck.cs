using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib._00_Import;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    // ReSharper disable once InconsistentNaming
    public class C_CorrectHausanschlussAssignmentCheck : RunableForSingleSliceWithBenchmark {
        public C_CorrectHausanschlussAssignmentCheck([NotNull] ServiceRepository services) : base(nameof(C_CorrectHausanschlussAssignmentCheck),
            Stage.ProfileGeneration,
            300,
            services,
            false,
            null)
        {
        }

        protected override void RunActualProcess(ScenarioSliceParameters slice)
        {
            if (!slice.Equals(Constants.PresentSlice)) {
                return;
            }

            var dbHouses = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
            Info("using house db in  " + dbHouses.ConnectionString);
            var houses = dbHouses.Fetch<House>();
            HouseComponentRepository hcr = new HouseComponentRepository(dbHouses);
            var hausanschlusses = dbHouses.Fetch<Hausanschluss>();
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var suppIsn = dbRaw.Fetch<HausanschlussImportSupplement>();
            var standortDict = new Dictionary<string, string>();
            var rlms = dbRaw.Fetch<RlmProfile>();
            List<AssignmentEntry> ases = new List<AssignmentEntry>();
            foreach (var supplement in suppIsn) {
                if (!string.IsNullOrWhiteSpace(supplement.TargetStandort)) {
                    if (standortDict.ContainsKey(supplement.TargetStandort)) {
                        throw new FlaException("Already contains standort " + supplement.TargetStandort);
                    }

                    standortDict.Add(supplement.TargetStandort, supplement.HaObjectid);
                }

                AssignmentEntry ase = ases.FirstOrDefault(x => x.ObjectId == supplement.HaObjectid);
                if (ase == null) {
                    ase = new AssignmentEntry(supplement.HaObjectid);
                    ases.Add(ase);
                }

                ase.Targets.Add(supplement.TargetStandort);
                ase.TargetCount++;
            }

            List<string> checkedStandorte = new List<string>();
            var lgzs = ReadZuordnungLastgänge();
            var successfullAssignments = new List<LastgangZuordnung>();
            RowCollection rc = new RowCollection("sheet", "Sheet1");
            List<PvSystemEntry> assignedPVsystems = new List<PvSystemEntry>();
            List<PvSystemEntry> otherPVsystems = new List<PvSystemEntry>();
            foreach (House house in houses) {
                var houseComponents = house.CollectHouseComponents(hcr);
                foreach (var component in houseComponents) {
                    if (component.Standort == null) {
                        continue;
                    }

                    if (standortDict.ContainsKey(component.Standort)) {
                        Hausanschluss ha = hausanschlusses.Single(x => x.Guid == component.HausAnschlussGuid);
                        string targetObjectID = standortDict[component.Standort];
                        if (ha.ObjectID != targetObjectID) {
                            throw new FlaException("Incorrect hausanschluss for " + component.Name + ": was supposed to be " + targetObjectID +
                                                   " but instead was " + ha.ObjectID + " (standort: " + component.Standort);
                        }

                        var ase = ases.Single(x => x.ObjectId == ha.ObjectID);
                        ase.AssignedCount++;
                        ase.Assigned.Add(component.Standort);
                        checkedStandorte.Add(component.Standort);
                    }

                    if (component.HouseComponentType == HouseComponentType.BusinessWithLastgangHighVoltage) {
                        var businessEntry = (BusinessEntry)component;
                        var rlmfn = businessEntry.RlmProfileName ?? throw new FlaException("No file");
                        var ha = hausanschlusses.First(x => x.Guid == businessEntry.HausAnschlussGuid);
                        var rlm = rlms.Single(x => x.Name == businessEntry.RlmProfileName);
                        var energysum = new Profile(rlm.Profile).EnergySum();
                        LogAssignment(lgzs,
                            rlmfn,
                            ha,
                            successfullAssignments,
                            rc,
                            businessEntry.Name,
                            businessEntry.Standort,
                            suppIsn,
                            "HS",
                            businessEntry.EffectiveEnergyDemand,
                            energysum,
                            house.ComplexName,
                            businessEntry.FinalIsn);
                    }

                    if (component.HouseComponentType == HouseComponentType.BusinessWithLastgangLowVoltage) {
                        var businessEntry = (BusinessEntry)component;
                        var rlmfn = businessEntry.RlmProfileName ?? throw new FlaException("no file?");
                        var ha = hausanschlusses.First(x => x.Guid == businessEntry.HausAnschlussGuid);
                        var rlm = rlms.Single(x => x.Name == businessEntry.RlmProfileName);
                        var energysum = new Profile(rlm.Profile).EnergySum();
                        LogAssignment(lgzs,
                            rlmfn,
                            ha,
                            successfullAssignments,
                            rc,
                            businessEntry.Name,
                            businessEntry.Standort,
                            suppIsn,
                            "NS",
                            businessEntry.EffectiveEnergyDemand,
                            energysum,
                            house.ComplexName,
                            businessEntry.FinalIsn);
                    }

                    if (component.HouseComponentType == HouseComponentType.Kwkw) {
                        var kwkw = (KleinWasserkraft)component;
                        var rlmfn = kwkw.RlmProfileName;
                        var ha = hausanschlusses.First(x => x.Guid == kwkw.HausAnschlussGuid);
                        var rlm = rlms.Single(x => x.Name == kwkw.RlmProfileName);
                        var energysum = new Profile(rlm.Profile).EnergySum();
                        LogAssignment(lgzs,
                            rlmfn,
                            ha,
                            successfullAssignments,
                            rc,
                            kwkw.Name,
                            kwkw.Standort,
                            suppIsn,
                            "WKW",
                            kwkw.EffectiveEnergyDemand,
                            energysum,
                            house.ComplexName,
                            kwkw.FinalIsn);
                    }

                    if (component.HouseComponentType == HouseComponentType.Photovoltaik) {
                        var ha = hausanschlusses.First(x => x.Guid == component.HausAnschlussGuid);
                        if (lgzs.Any(x => string.Equals(x.Knoten.ToLower(), ha.ObjectID.ToLower(), StringComparison.InvariantCultureIgnoreCase))) {
                            assignedPVsystems.Add((PvSystemEntry)component);
                        }
                        else {
                            otherPVsystems.Add((PvSystemEntry)component);
                        }
                    }
                }
            }

            foreach (var zuordnung in lgzs) {
                if (!successfullAssignments.Contains(zuordnung)) {
                    RowBuilder rb = RowBuilder.Start("Lastgang", zuordnung.FileName).Add("Diren Profilename", zuordnung.Knoten);
                    var has = hausanschlusses.Where(x => x.ObjectID == zuordnung.Knoten).ToList();
                    var haguids = has.Select(x => x.Guid).Distinct().ToList();
                    var pvs = assignedPVsystems.Where(x => haguids.Contains(x.HausAnschlussGuid)).ToList();
                    var houseGuids = has.Select(x => x.HouseGuid).Distinct().ToList();
                    var otherPV = otherPVsystems.Where(x => houseGuids.Contains(x.HouseGuid));
                    rb.Add("PV Systems", string.Join(";", pvs.Select(x => x.Name)));
                    rb.Add("Other PV Systems@house", string.Join(";", otherPV.Select(x => x.Name)));
                    var rlm = rlms.Where(x => x.Name.Contains(zuordnung.FileName)).ToList();
                    for (int i = 0; i < rlm.Count; i++) {
                        var energysum = new Profile(rlm[i].Profile).EnergySum();
                        rb.Add("Profilesumme " + i, energysum);
                    }

                    rc.Add(rb);
                    if (has.Count > 0) {
                        List<string> housenames = new List<string>();
                        foreach (var ha in has) {
                            var house = houses.First(x => x.Guid == ha.HouseGuid);
                            housenames.Add(house.ComplexName);
                        }

                        rb.Add("Hausname", string.Join(",", housenames.Distinct()));
                    }
                }
            }

            foreach (var pVsystem in otherPVsystems) {
                House house = houses.First(x => x.Guid == pVsystem.HouseGuid);
                var rb = RowBuilder.Start("Hausname", house.ComplexName);
                rb.Add("Effective Energy", pVsystem.EffectiveEnergyDemand);
                rb.Add("Planned Energy", pVsystem.EffectiveEnergyDemand);
                var ha = hausanschlusses.First(x => x.Guid == pVsystem.HausAnschlussGuid);
                rb.Add("Zugeordnete ObjektID", ha.ObjectID);
                rc.Add(rb);
            }

            var fn = MakeAndRegisterFullFilename("WrongAssignments.xlsx", slice);
            XlsxDumper.WriteToXlsx(fn, rc);

            var missingAssigments = lgzs.Where(x => !successfullAssignments.Contains(x)).ToList();
            if (missingAssigments.Count > 0) {
                Info("Missing assignments: " + string.Join("\n", missingAssigments.Select(x => x.FileName)));
            }

            foreach (var pair in standortDict) {
                if (!checkedStandorte.Contains(pair.Key)) {
                    //throw new FlaException("Didn't find supplemental standort " + pair.Value + " in the list of components. Typo?");
                }
            }

            foreach (var ase in ases) {
                foreach (var target in ase.Targets) {
                    if (!ase.Assigned.Contains(target)) {
                      //  throw new FlaException("Missing " + target + " from the list of assigned standorts");
                    }
                }

                foreach (var assigned in ase.Assigned) {
                    if (!ase.Targets.Contains(assigned)) {
                        //throw new FlaException("Missing " + assigned + " from the list of target standorts");
                    }
                }

            }
        }

        private static void LogAssignment([NotNull] [ItemNotNull] List<LastgangZuordnung> lgzs,
                                          [NotNull] string rlmfn,
                                          [NotNull] Hausanschluss ha,
                                          [NotNull] [ItemNotNull] List<LastgangZuordnung> successfullAssignments,
                                          [NotNull] RowCollection rc,
                                          [NotNull] string name,
                                          [NotNull] string standort,
                                          [NotNull] [ItemNotNull] List<HausanschlussImportSupplement> supps,
                                          [NotNull] string srctype,
                                          double targetSum,
                                          double assignedSum,
                                          [NotNull] string housename,
                                          int isn)
        {
            var lgz = lgzs.First(x => rlmfn.Contains(x.FileName));
            successfullAssignments.Add(lgz);
            RowBuilder rb = new RowBuilder();
            rc.Add(rb);
            rb.Add("Typ", srctype);
            rb.Add("Name", name);
            rb.Add("Hausname", housename);
            rb.Add("Standort", standort);
            rb.Add("ISN", isn);
            rb.Add("Zugeordnete ObjektID", ha.ObjectID);
            rb.Add("MyFile", rlmfn);
            rb.Add("Profilename", lgz.FileName);
            rb.Add("Diren Knoten", lgz.Knoten);
            rb.Add("Abrechnungssumme", targetSum);
            rb.Add("Profilesumme", assignedSum);
            rb.Add("Skalierungsfaktor", targetSum / assignedSum);
            if (!string.Equals(ha.ObjectID, lgz.Knoten, StringComparison.CurrentCultureIgnoreCase)) {
                rb.Add("Zuordnung", "Anders");
            }

            var supp = supps.FirstOrDefault(x => x.TargetStandort == standort);
            if (supp != null) {
                rb.Add("Explizite Zuordnung", "ja");
            }
        }

        [NotNull]
        [ItemNotNull]
        private List<LastgangZuordnung> ReadZuordnungLastgänge()
        {
            List<LastgangZuordnung> lgzs = new List<LastgangZuordnung>();
            string xlsfn = CombineForFlaSettings("ZuordnungLastgängeFinal.xlsx");
            ExcelHelper eh = new ExcelHelper(Services.Logger, MyStage);
            var arr = eh.ExtractDataFromExcel2(xlsfn, 1, "A1", "F89", out var _);
            int row = 1;
            while (arr[row, 4] != null && row < arr.GetLength(0)) {
                string fn = (string)arr[row, 3] ?? throw new FlaException("was null");
                string objectid = (string)arr[row, 5] ?? throw new FlaException("was null");
                var lgz = new LastgangZuordnung(fn, objectid);
                lgzs.Add(lgz);
                row++;
            }

            return lgzs;
        }

        private class AssignmentEntry {
            public AssignmentEntry([NotNull] string objectId) => ObjectId = objectId;

            [NotNull]
            [ItemNotNull]
            public List<string> Assigned { get; } = new List<string>();

            public int AssignedCount { get; set; }

            [NotNull]
            public string ObjectId { get; }

            public int TargetCount { get; set; }

            [NotNull]
            [ItemNotNull]
            public List<string> Targets { get; } = new List<string>();
        }

        private class LastgangZuordnung {
            public LastgangZuordnung([NotNull] string fileName, [NotNull] string knoten)
            {
                FileName = fileName;
                Knoten = knoten;
            }

            [NotNull]
            public string FileName { get; set; }

            [NotNull]
            public string Knoten { get; set; }
        }
    }
}
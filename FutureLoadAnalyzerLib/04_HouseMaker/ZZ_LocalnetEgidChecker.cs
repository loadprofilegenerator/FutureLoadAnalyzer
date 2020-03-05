/*using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.DataModel;
using BurgdorfStatistics.DataModel.Creation;
using BurgdorfStatistics.DataModel.Dst;
using BurgdorfStatistics.Tooling;
using Newtonsoft.Json;

namespace BurgdorfStatistics._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class ZZ_LocalnetEgidChecker : RunableWithBenchmark {
        public ZZ_LocalnetEgidChecker([JetBrains.Annotations.NotNull] ServiceRepository services)
            : base(nameof(ZZ_LocalnetEgidChecker), Stage.Houses, 3000, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            SqlConnection.RecreateTable<LocalnetEgidZuordnung>(Stage.Houses, Constants.PresentSlice);
            var dbComplex = SqlConnection.GetDatabaseConnection(Stage.Complexes, Constants.PresentSlice);
            var dbRaw = SqlConnection.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var buildingcomplexes = dbComplex.Fetch<BuildingComplex>();
            var dbHouse = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var complexesByID = new Dictionary<string, BuildingComplex>();
            dbHouse.BeginTransaction();
            var hausanschluesse = dbRaw.Fetch<Hausanschluss>();
            foreach (var complex in buildingcomplexes) {
                complexesByID.Add(complex.ComplexGuid, complex);
            }

            foreach (var h in houses) {
                var complex = complexesByID[h.ComplexGuid];
                var complexEgids = complex.EGids.Where(x => x > 0).ToList();
                var localnetEgid = new LocalnetEgidZuordnung(complex.ComplexName, JsonConvert.SerializeObject(complexEgids), "", complex.GebäudeObjectIDsAsJson) {
                    LocalnetEGids = new List<long>()
                };
                var save = false;
                foreach (var gebäudeObjectID in complex.GebäudeObjectIDs) {
                    var has = hausanschluesse.Where(x => x.u_obj_id_i == gebäudeObjectID).ToList();
                    if (has.Count > 0) {
                        save = true;
                    }

                    foreach (var ha in has) {
                        if (!localnetEgid.LocalnetEGids.Contains(ha.u_egid_ise) && ha.u_egid_ise != 0) {
                            localnetEgid.LocalnetEGids.Add(ha.u_egid_ise);
                        }

                        var adresse = ha.u_strasse1 + " " + ha.u_str_nr_i;
                        if (!localnetEgid.GISHausanschlussadresse.Contains(adresse)) {
#pragma warning disable CC0039 // Don't concatenate strings in loops
                            localnetEgid.GISHausanschlussadresse += adresse + " ";
#pragma warning restore CC0039 // Don't concatenate strings in loops
                        }
                    }
                }

                var nurLocalnet = localnetEgid.LocalnetEGids.Except(complexEgids).ToList();
                var nurComplexes = complexEgids.Except(localnetEgid.LocalnetEGids).ToList();
                if (!nurLocalnet.Any() && !nurComplexes.Any()) {
                    localnetEgid.Status = "Gleich";
                }
                else if (nurLocalnet.Any() && nurComplexes.Any()) {
                    localnetEgid.Status = "Ungleich";
                }
                else if (nurLocalnet.Any() && !nurComplexes.Any()) {
                    localnetEgid.Status = "Nur Localnet";
                }
                else if (!nurLocalnet.Any() && nurComplexes.Any()) {
                    localnetEgid.Status = "Nur BFH";
                }
                else {
                    throw new Exception("unknown");
                }

                if (save) {
                    dbHouse.Save(localnetEgid);
                }
            }

            dbHouse.CompleteTransaction();
        }
    }
}*/
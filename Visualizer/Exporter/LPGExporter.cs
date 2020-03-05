//using System;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Automation;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace BurgdorfStatistics.Exporter {
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class LPGExporter : RunableWithBenchmark {
        public LPGExporter([NotNull] ServiceRepository services)
            : base(nameof(LPGExporter), Stage.ValidationExporting, 5, services, false)
        {
        }

        protected override void RunActualProcess()
        {
            var dbHouses = SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice).Database;
            var houses = dbHouses.Fetch<House>();
            var households = dbHouses.Fetch<Household>();
            var occupants = dbHouses.Fetch<Occupant>();
            var tks = houses.Select(x => x.TrafoKreis).Distinct().ToList();
            HashSet<string> usedKeys  = new HashSet<string>();
            foreach (var tk in tks) {
                if (string.IsNullOrWhiteSpace(tk)) {
                    continue;
                }

                var allHouses = new List<HouseData>();
                var filteredhouses = houses.Where(x => x.TrafoKreis == tk).ToList();
                foreach (var house in filteredhouses) {
                    var myhouseholds = households.Where(x => x.HouseGuid == house.HouseGuid).ToList();
                    if (myhouseholds.Count == 0) {
                        continue;
                    }

                    var hd = new HouseData(house.HouseGuid, GetHousetype(house), 1000, 0, house.ComplexName);
                    var houseidx = 0;
                    foreach (var household in myhouseholds) {
                        if (usedKeys.Contains(household.HouseholdKey)) {
                            throw new Exception("Household key was already in use: " + household.HouseholdKey + " - TK " + tk);
                        }

                        usedKeys.Add(household.HouseholdKey);
                        var hhd = new HouseholdData(household.HouseholdKey,
                            household.LowVoltageYearlyTotalElectricityUse, 10, ElectricCarUse.NoElectricCar, house.ComplexName + " " + houseidx++,
                            null,null,null);
                        var myOccupants = occupants.Where(x => x.HouseholdGuid == household.HouseholdGuid).ToList();
                        foreach (var occupant in myOccupants) {
                            var g = (Automation.Gender)occupant.Gender;
                            hhd.Persons.Add(new PersonData(occupant.Age, g));
                        }

                        hd.Households.Add(hhd);
                    }

                    //TODO: make load profiles for street lights
                    allHouses.Add(hd);
                }

                var filename = MakeAndRegisterFullFilename("HouseDataExport." + tk + ".json", Name, "", Constants.PresentSlice);
                var sw = new StreamWriter(filename);
                var tkd = new DistrictData {
                    Name = tk,
                    Houses = allHouses
                };
                sw.WriteLine(JsonConvert.SerializeObject(tkd, Formatting.Indented));
                sw.Close();
            }
        }

        [NotNull]
        public string GetHousetype([NotNull] House house) => "HT01";
    }
}
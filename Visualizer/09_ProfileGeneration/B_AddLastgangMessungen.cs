using System;
using System.Collections.Generic;
using System.Linq;
using BurgdorfStatistics.Logging;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.ProfileImport;
using JetBrains.Annotations;

namespace BurgdorfStatistics._09_ProfileGeneration {
    // ReSharper disable once InconsistentNaming
    public class B_AddLastgangMessungen : RunableForSingleSliceWithBenchmark {
        public B_AddLastgangMessungen([NotNull] ServiceRepository services)
            : base(nameof(B_AddLastgangMessungen), Stage.ProfileGeneration, 200, services, false)
        {
            DevelopmentStatus.Add("//todo: adjust with new factors, for example more efficient business");
            DevelopmentStatus.Add("//todo: adjust with new factors");
            DevelopmentStatus.Add("//todo: deal properly with houses with multiple isns");
        }

        private readonly bool skipAllRlm = true;

        protected override void RunActualProcess([NotNull] ScenarioSliceParameters parameters)
        {
            Prosumer.ClearProsumerTypeFromDB(Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration,
                parameters),  ProsumerType.LastgangGeneration, TableType.HousePart);
            Prosumer.ClearProsumerTypeFromDB(Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration,
                parameters),  ProsumerType.BusinessWithLastgang, TableType.HousePart);
            var dbSrcProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
            var dbHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, parameters).Database;
            var dbDstProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGeneration, parameters);
            var assignments = dbSrcProfiles.Fetch<LastgangBusinessAssignment>();

            var profiles = dbSrcProfiles.Fetch<RlmProfile>();
            var houses = dbHouses.Fetch<House>();
            var businesses = dbHouses.Fetch<BusinessEntry>();
            //var hausAnschlussses = dbHouses.Fetch<Hausanschluss>();
            Log(MessageType.Info, "making " + assignments.Count + " assignments");
            var sa = Prosumer.GetSaveableEntry(dbDstProfiles, TableType.HousePart);
            if (skipAllRlm) {
                return;
            }

            foreach (var assignment in assignments) {
                if (assignment.BusinessName == "none") {
                    continue;
                }

                //pv anlagen
                if (!string.IsNullOrWhiteSpace(assignment.ErzeugerID)) {
                    if (MakeErzeugerLastgang(houses, assignment, profiles, sa)) {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(assignment.ComplexName) && !string.IsNullOrWhiteSpace(assignment.BusinessName)) {
                    var selectedBusinesses = businesses.Where(x => x.BusinessName == assignment.BusinessName).ToList();
                    if (selectedBusinesses.Count == 0) {
                        throw new Exception("Not a single business was found: " + assignment.BusinessName);
                    }

                    var rightbusiness = selectedBusinesses[0];
                    if (assignment.Standort != null && selectedBusinesses.Count > 1) {
                        rightbusiness = selectedBusinesses.Single(x => x.Standorte.Contains(assignment.Standort));
                    }

                    if (selectedBusinesses.Count > 1) {
                        var rightHouse = houses.Single(x => x.ComplexName == assignment.ComplexName);
                        selectedBusinesses = selectedBusinesses.Where(x => x.HouseGuid == rightHouse.HouseGuid).ToList();
                        rightbusiness = selectedBusinesses[0];
                    }

                    var house = houses.Single(x => x.HouseGuid == rightbusiness.HouseGuid);
                    int isnid = -1;
                    if (house.GebäudeObjectIDs.Count > 0) {
                        isnid = house.GebäudeObjectIDs[0];
                    }

                    //if (house.GebäudeObjectIDs.Count > 1) {
                        //throw new Exception("trying to export entry for house with more than one isn id");
                    //}
                    var has = house.Hausanschluss.FirstOrDefault(x => x.Isn == isnid);
                    if (has == null) {
                        throw new FlaException("No hausanschluss");
                    }
                    var pa = new Prosumer(house.HouseGuid, assignment.RlmFilename, ProsumerType.BusinessWithLastgang,
                        rightbusiness.BusinessGuid, isnid, has.HausanschlussGuid,has.ObjectID);
                    var rlmprofile = profiles.Single(x => x.Name == assignment.RlmFilename);
                    pa.Profile = rlmprofile.Profile;
                    sa.AddRow(pa);
                }
            }

            sa.SaveDictionaryToDatabase();
        }

        private bool MakeErzeugerLastgang([NotNull] [ItemNotNull] List<House> houses, [NotNull] LastgangBusinessAssignment assignment, [NotNull] [ItemNotNull] List<RlmProfile> profiles,
                                          [NotNull] SaveableEntry<Prosumer> sa)
        {
            var selectedhouses = houses.Where(x => x.ErzeugerIDs.Contains(assignment.ErzeugerID)).ToList();
            if (selectedhouses.Count != 1) {
                if (selectedhouses.Count == 0) {
                    Log(MessageType.Info, "No house found for " + assignment.ErzeugerID);
                    return true;
                }

                throw new Exception(selectedhouses.Count + " houses for erzeuger id " + assignment.ErzeugerID);
            }

            Hausanschluss ha = selectedhouses[0].Hausanschluss[0];
            //odo: adjust with new factors
            var rlmrprofile = profiles.Single(x => x.Name == assignment.RlmFilename);
            var pa = new Prosumer(selectedhouses[0].HouseGuid, assignment.RlmFilename,
                ProsumerType.LastgangGeneration, null,  selectedhouses[0].GebäudeObjectIDs[0], ha.HausanschlussGuid, ha.ObjectID) {
                Profile = rlmrprofile.Profile
            };
            sa.AddRow(pa);
            return false;
        }

        protected override void RunChartMaking([NotNull] ScenarioSliceParameters parameters)
        {
            /*  double min = 0;
              var dbSrcProfiles = Services.SqlConnection
                  .GetDatabaseConnection(Stage.ProfileImport, Constants.PresentSlice).Database;
              {
                  List<LineSeriesEntry> allLs = new List<LineSeriesEntry>();
                  List<BkwProfile> bkws = dbSrcProfiles.Fetch<BkwProfile>();
                  BkwProfile bkw = bkws[0];
                  LineSeriesEntry ls = bkw.Profile.GetLineSeriesEntry();
                  allLs.Add(ls);
  
                  var filename = MakeAndRegisterFullFilename("Profile_BKW.png", Name, "");
  
                  Services.PlotMaker.MakeLineChart(filename, bkw.Name, allLs,
                      new List<PlotMaker.AnnotationEntry>(), min);
              }
  
              {
                  var dbGEneratedProfiles = Services.SqlConnection
                      .GetDatabaseConnection(Stage.ProfileGeneration, parameters.DstScenario, parameters.DstYear).Database;
  
                  List<LineSeriesEntry> allLs = new List<LineSeriesEntry>();
                  var residual = dbGEneratedProfiles.Fetch<ResidualProfile>();
                  LineSeriesEntry ls = residual[0].Profile.GetLineSeriesEntry();
                  allLs.Add(ls);
                  min = Math.Min(0, residual[0].Profile.Values.Min());
  
                  var filename = MakeAndRegisterFullFilename("Profile_Residual.png", Name, "");
  
                  Services.PlotMaker.MakeLineChart(filename, residual[0].Name, allLs,
                      new List<PlotMaker.AnnotationEntry>(), min);
              }
  
              var rlms = dbSrcProfiles.Fetch<RlmProfile>();
              foreach (var rlm in rlms)
              {
                  List<LineSeriesEntry> allLs = new List<LineSeriesEntry>();
  
                  LineSeriesEntry ls1 = rlm.Profile.GetLineSeriesEntry();
                  allLs.Add(ls1);
  
                  var filename = MakeAndRegisterFullFilename("Profile." + rlm.Name + ".png", Name, "");
                  min = Math.Min(0, rlm.Profile.Values.Min());
                  Services.PlotMaker.MakeLineChart(filename, rlm.Name, allLs,
                      new List<PlotMaker.AnnotationEntry>(), min);
              }*/
        }
    }
}
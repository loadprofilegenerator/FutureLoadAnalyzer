using System;
using System.Diagnostics;
using System.Linq;
using Autofac;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using Data.DataModel.Creation;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._05_ScenarioCreation {
    public class A02PersistenceTest {
        public A02PersistenceTest([CanBeNull] ITestOutputHelper output) => _output = output;

        [CanBeNull] private readonly ITestOutputHelper _output;

        private static bool IsInvalidMember([NotNull] IMemberInfo x)
        {
            if (x.SelectedMemberPath.EndsWith("OccupantID")) {
                return true;
            }

            if (x.SelectedMemberPath.EndsWith(".OccupantGuid")) {
                return true;
            }

            if (x.SelectedMemberPath.Contains(".OccupantGuid")) {
                return true;
            }

            if (x.SelectedMemberPath.Contains(".OccupantsAsJson")) {
                return true;
            }

            return false;
        }

        private static int OccupantComparison([NotNull] Occupant x, [NotNull] Occupant y)
        {
            if (x.HouseholdKey != y.HouseholdKey) {
                return string.Compare(x.HouseholdKey, y.HouseholdKey, StringComparison.Ordinal);
            }

            if (x.Age != y.Age) {
                return x.Age.CompareTo(y.Age);
            }

            if (x.Gender != y.Gender) {
                return x.Gender.CompareTo(y.Gender);
            }

            return 0;
        }

        [Fact]
        public void RunA02PersistenceTest()
        {
            _output?.WriteLine("This is output from {0}", "RunSingleParameterSlice");
            RunningConfig settings = RunningConfig.MakeDefaults();
            settings.MyOptions.Add(Options.ReadFromExcel);
            settings.LimitToYears.Add(2020);
            settings.LimitToScenarios.Add(Scenario.FromEnum(ScenarioEnum.Utopia));
            using (Logger logger = new Logger(_output, settings)) {
                settings.InitializeSlices(logger);
                using (var container = MainBurgdorfStatisticsCreator.CreateBuilderContainer(_output, logger, settings)) {
                    using (var scope = container.BeginLifetimeScope()) {
                        if (settings.Slices == null) {
                            throw new FlaException("slices was not initalized");
                        }

                        ServiceRepository services = scope.Resolve<ServiceRepository>();
                        var s1 = scope.Resolve<A02_ScnHouseholds>();
                        var slice = settings.Slices[0];
                        s1.RunForScenarios(slice);
                        var db = services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, slice);
                        var households1 = db.Fetch<Household>();
                        var occupants1 = households1.SelectMany(x => x.Occupants).ToList();
                        var s2 = scope.Resolve<A02_ScnHouseholds>();
                        s2.RunForScenarios(slice);
                        var households2 = db.Fetch<Household>();
                        var occupants2 = households2.SelectMany(x => x.Occupants).ToList();
                        SLogger.Info("Finished reading, now comparing entries");
                        var sw = Stopwatch.StartNew();
                        households1.Sort((x, y) => string.Compare(x.HouseholdKey, y.HouseholdKey, StringComparison.Ordinal));
                        households2.Sort((x, y) => string.Compare(x.HouseholdKey, y.HouseholdKey, StringComparison.Ordinal));
                        occupants1.Sort(OccupantComparison);
                        occupants2.Sort(OccupantComparison);
                        households1.Should().HaveCount(households2.Count);
                        occupants1.Should().HaveCount(occupants2.Count);
                        SLogger.Info("starting compare took " + sw.Elapsed);
                        //compare
                        var occ1A = occupants1.Take(10).ToList();
                        var occ2A = occupants2.Take(10).ToList();
                        occ1A.Should().BeEquivalentTo(occ2A, config => config.Excluding(s => IsInvalidMember(s)).ExcludingNestedObjects());
                        var hh1A = households1.Take(10).ToList();
                        var hh2A = households2.Take(10).ToList();
                        hh1A.Should().BeEquivalentTo(hh2A, config => config.Excluding(s => IsInvalidMember(s)));
                        SLogger.Info("comparing took " + sw.Elapsed);
                    }
                }
            }
        }
    }
}
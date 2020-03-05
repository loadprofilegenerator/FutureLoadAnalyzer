using System;
using System.Collections.Generic;
using System.Linq;
using Common.Steps;
using Data.DataModel;
using Data.DataModel.Creation;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Steps;
using JetBrains.Annotations;
using Visualizer.OSM;
using Visualizer.Sankey;
using Constants = Common.Constants;

namespace FutureLoadAnalyzerLib._04_HouseMaker {
    // ReSharper disable once InconsistentNaming
    public class A10_TotalGeographicMatchVisualizer : RunableWithBenchmark {
        public A10_TotalGeographicMatchVisualizer([NotNull] ServiceRepository services)
            : base(nameof(A10_TotalGeographicMatchVisualizer), Stage.Houses, 10, services, false)
        {
        }

        protected override void RunActualProcess()
        {
        }

        protected override void RunChartMaking()
        {
            var dbHouse = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var houses = dbHouse.Fetch<House>();
            var finishedMatches = dbHouse.Fetch<HouseOsmMatch>();
            var matchTypes = new Dictionary<string, List<MatchType>>();
            foreach (var match in finishedMatches) {
                if (!matchTypes.ContainsKey(match.OsmGuid)) {
                    matchTypes.Add(match.OsmGuid, new List<MatchType>());
                }

                if (!matchTypes[match.OsmGuid].Contains(match.MatchType)) {
                    matchTypes[match.OsmGuid].Add(match.MatchType);
                }
            }

            MakeSankeyPerMatchTypeSankey(Constants.PresentSlice);
            MakeGeneralMatching();
            MakeMatchingColor(Constants.PresentSlice);
            MakeMatchedHousesSankey(Constants.PresentSlice);

            void MakeGeneralMatching()
            {
                var colorByMatch = new List<MapColorEntryWithOsmGuid>();
                foreach (var pair in matchTypes) {
                    colorByMatch.Add(new MapColorEntryWithOsmGuid(pair.Key, Constants.Green));
                }

                var filename = MakeAndRegisterFullFilename("GeneralMatchingMap.png",Constants.PresentSlice);
                var labels = new List<MapLegendEntry> {
                    new MapLegendEntry("Direkt Gemappt", Constants.Green),
                    new MapLegendEntry("Nicht Gemappt", Constants.Red)
                };

                Services.PlotMaker.MakeOsmMap(Name, filename, colorByMatch, new List<WgsPoint>(), labels, new List<LineEntry>());
            }

            void MakeMatchedHousesSankey(ScenarioSliceParameters slice)
            {
                var matchedHouses = new List<House>();
                var houseKeys = finishedMatches.Select(x => x.HouseGuid).Distinct().ToHashSet();
                foreach (var house in houses) {
                    if (houseKeys.Contains(house.Guid)) {
                        matchedHouses.Add(house);
                    }
                }

                var ssa = new SingleSankeyArrow("MatchedHouseCount", 1000, MyStage,
                    SequenceNumber, Name, slice, Services);
                ssa.AddEntry(new SankeyEntry("Total Houses", houses.Count, 5000, Orientation.Straight));
                ssa.AddEntry(new SankeyEntry("Mapped", matchedHouses.Count * -1, 5000, Orientation.Up));
                ssa.AddEntry(new SankeyEntry("Sonstiges", (houses.Count - matchedHouses.Count) * -1, 5000, Orientation.Down));
                Services.PlotMaker.MakeSankeyChart(ssa);
            }

            void MakeSankeyPerMatchTypeSankey(ScenarioSliceParameters slice)
            {
                foreach (MatchType matchType in Enum.GetValues(typeof(MatchType))) {
                    var matches = finishedMatches.Where(x => x.MatchType == matchType).Select(x => x.HouseGuid).Distinct().ToList();

                    var ssa = new SingleSankeyArrow("MatchedHouseCount." + matchType.ToString(), 1000,
                        MyStage, SequenceNumber, Name, slice, Services);
                    ssa.AddEntry(new SankeyEntry("Total Houses", houses.Count, 5000, Orientation.Straight));
                    ssa.AddEntry(new SankeyEntry("Mapped to " + matchType, matches.Count * -1, 5000, Orientation.Up));
                    ssa.AddEntry(new SankeyEntry("Sonstiges", (houses.Count - matches.Count) * -1, 5000, Orientation.Down));
                    Services.PlotMaker.MakeSankeyChart(ssa);
                }
            }

            void MakeMatchingColor(ScenarioSliceParameters slice)
            {
                var colorByMatch = new List<MapColorEntryWithOsmGuid>();
                foreach (var pair in matchTypes) {
                    if (pair.Value.Count > 2) {
                        colorByMatch.Add(new MapColorEntryWithOsmGuid(pair.Key, Constants.Green));
                    }
                    else if (pair.Value.Count > 1) {
                        colorByMatch.Add(new MapColorEntryWithOsmGuid(pair.Key, Constants.Blue));
                    }
                    else if (pair.Value.Count > 0) {
                        colorByMatch.Add(new MapColorEntryWithOsmGuid(pair.Key, Constants.Türkis));
                    }
                }

                var filename = MakeAndRegisterFullFilename("MapByNumberOfMatches.png", slice);
                var labels = new List<MapLegendEntry> {
                    new MapLegendEntry("Drei Matches", Constants.Green),
                    new MapLegendEntry("Zwei Matches", Constants.Blue),
                    new MapLegendEntry("Ein Match", Constants.Türkis),
                    new MapLegendEntry("Nicht Gemappt", Constants.Red)
                };
                Services.PlotMaker.MakeOsmMap(Name, filename, colorByMatch, new List<WgsPoint>(), labels, new List<LineEntry>());
            }
        }
    }
}
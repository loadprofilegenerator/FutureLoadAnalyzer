using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.HeatingProviders {
    public class DhwProfileProviderTest : UnitTestBase {
        public DhwProfileProviderTest([CanBeNull] ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void RunSingleHpTest()
        {
            DhwProfileGenerator chp = new DhwProfileGenerator();
            Random rnd = new Random();
            var dhwcp = DhwCalculationParameters.MakeDefaults();
            const double energyuse = 20000;
            var hpr = chp.Run(dhwcp, energyuse, rnd);
            var profile = hpr.GetEnergyDemandProfile();
            const string file = "dhwProfile_Single.xlsx";
            var colnames = new List<string> {"turnoff"};
            XlsxDumper.DumpProfilesToExcel(file,
                2017,
                15,
                new ProfileWorksheetContent("sheet1", "Last", 240, profile),
                new EnumWorksheetContent<DhwTurnOffReason>("turnoffs", colnames, hpr.TurnOffReasons.AsReadOnly()));
            profile.Values.Sum().Should().BeApproximately(energyuse, 100);
        }
    }
}
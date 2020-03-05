using System;
using System.Diagnostics.CodeAnalysis;
using Data.DataModel.ProfileImport;
using Xunit;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class SlpProviderTest {
        [Fact]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
        public void Run()
        {

            var slp = new SLPProvider(2017,null,null);
            //winter
            Assert.Equal(Season.Winter, slp.GetSeason(new DateTime(2017, 3, 20)));
            Assert.Equal(Season.Winter, slp.GetSeason(new DateTime(2017, 11, 1)));
            Assert.Equal(Season.Winter, slp.GetSeason(new DateTime(2017, 1, 1)));
            Assert.Equal(Season.Winter, slp.GetSeason(new DateTime(2017, 12, 31)));
            //sommer
            Assert.Equal(Season.Sommer, slp.GetSeason(new DateTime(2017, 05, 15)));
            Assert.Equal(Season.Sommer, slp.GetSeason(new DateTime(2017, 07, 20)));
            Assert.Equal(Season.Sommer, slp.GetSeason(new DateTime(2017, 09, 14)));
            //uebergangszeit
            Assert.Equal(Season.Uebergang, slp.GetSeason(new DateTime(2017, 3, 21)));
            Assert.Equal(Season.Uebergang, slp.GetSeason(new DateTime(2017, 4, 21)));
            Assert.Equal(Season.Uebergang, slp.GetSeason(new DateTime(2017, 05, 14)));
            Assert.Equal(Season.Uebergang, slp.GetSeason(new DateTime(2017, 9, 15)));
            Assert.Equal(Season.Uebergang, slp.GetSeason(new DateTime(2017, 10, 1)));
            Assert.Equal(Season.Uebergang, slp.GetSeason(new DateTime(2017, 10, 31)));
        }
    }
}
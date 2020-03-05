using Xunit;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.PVProfile {
    public class PVSystemKeyTester {
        [Fact]
        public void TestPVSystemKey()
        {
            PVSystemKey pk1 = new PVSystemKey(10,10, 2050);
            PVSystemKey pk2 = new PVSystemKey(10, 10, 2050);
            Assert.Equal(pk1,pk2);
            Assert.True(pk1==pk2);
        }
    }
}
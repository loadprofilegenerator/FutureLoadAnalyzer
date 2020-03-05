using System.IO;

namespace BurgdorfStatistics.Tooling {
    // ReSharper disable once UnusedMember.Global
    internal class StandardMinuteConverter {
        public void Run()
        {
            const string srcFile = @"V:\Dropbox\BurgdorfStatistics\t1\Bern-min.dat";
            var sr = new StreamReader(srcFile);
            const string dstfile = @"V:\Dropbox\BurgdorfStatistics\t1\Bern-min.csv";
            var sw = new StreamWriter(dstfile);
            while (!sr.EndOfStream) {
                var line = sr.ReadLine();
                //Year,Month,Day,Hour,Minute,GHI,DNI,DHI,Tdry,Tdew,RH,Pres,Wspd,Wdir,Snow Depth
                sw.WriteLine(line);
            }

            sr.Close();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Automation.ResultFiles;
using Common;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders.LPG {
    public static class ProfileLoader {
        [CanBeNull]
        public static Profile LoadProfiles([NotNull] ResultFileEntry rfe,
                                           [NotNull] string dstDirectory,
                                           [NotNull] out FileInfo loadedFile)
        {
            DirectoryInfo di = new DirectoryInfo(dstDirectory);
            var fis = di.GetFiles("*.json", SearchOption.AllDirectories);
            if (fis.Length == 0) {
                throw new FlaException("Not a single json file was found.");
            }

            if (rfe.ResultFileID != ResultFileID.ExternalSumsForHouseholdsJson) {
                throw new FlaException("Invalid result file id");
            }

            var fi = fis.Single(x => x.Name == rfe.FileName);
            loadedFile = fi;
            string json = File.ReadAllText(fi.FullName);
            List<double> values = JsonConvert.DeserializeObject<List<double>>(json);

            Profile p = new Profile(rfe.HouseholdKey, values.AsReadOnly(), EnergyOrPower.Energy);
            p = p.AdjustValueCountForLeapYear();
            return p;
        }
    }
}
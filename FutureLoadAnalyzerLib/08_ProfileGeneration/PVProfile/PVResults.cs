using System.Collections.Generic;
using System.Linq;
using Data.DataModel.Profiles;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.PVProfile {
    public class PVResults {
        public PVResults(double capacityFactor, double kwhPerKW, [NotNull] List<float> pvProfile,
                        double maxPower, PVSystemKey key)
        {
            //AnnualEnergy = annualEnergy;
            CapacityFactor = capacityFactor;
            KwhPerKW = kwhPerKW;
            PVProfile = pvProfile;
            MaxPower = maxPower;
            Key = key;
        }


        //public double AnnualEnergy { get; }
        public double CapacityFactor { [UsedImplicitly] get; }

        public double KwhPerKW { [UsedImplicitly] get; }

        public double MaxPower { [UsedImplicitly] get; }
        public PVSystemKey Key { get; }

        //public List<float> Tamb { get; set; }
        [NotNull]
        public List<float> PVProfile { [UsedImplicitly] get; }


        [NotNull]
        public Profile GetProfile()
        {
            return new Profile(Key.GetKey(), PVProfile.ConvertAll(x => (double)x).ToList().AsReadOnly(), EnergyOrPower.Power);
        }
    }
}
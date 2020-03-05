using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Steps;
using Data.DataModel.Creation;
using Data.DataModel.Export;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.LoadProfileProviders {
    public class LastgangMessungProvider : BaseProvider, ILoadProfileProvider {
        [NotNull] private readonly DBDto _dbdto;
        [NotNull] [ItemNotNull] private readonly HashSet<string> _usedKeys = new HashSet<string>();

        public LastgangMessungProvider([NotNull] ServiceRepository services, [NotNull] ScenarioSliceParameters slice, [NotNull] DBDto dbdto) : base(
            nameof(LastgangMessungProvider),
            services,
            slice) =>
            _dbdto = dbdto;

        public bool IsCorrectProvider(IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.BusinessWithLastgangLowVoltage) {
                return true;
            }

            if (houseComponent.HouseComponentType == HouseComponentType.BusinessWithLastgangHighVoltage) {
                return true;
            }

            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters) => true;

        [CanBeNull]
        protected override Prosumer ProvidePrivateProfile(ProviderParameterDto ppdto)
        {
            var businessEntry = (BusinessEntry)ppdto.HouseComponent;
            ppdto.HouseComponentResultObject.RlmFilename = businessEntry.RlmProfileName;
            if (businessEntry.HouseComponentType == HouseComponentType.BusinessWithLastgangLowVoltage) {
                var has = _dbdto.Hausanschlusse.Single(x => x.Guid == businessEntry.HausAnschlussGuid);
                string name = businessEntry.RlmProfileName ?? throw new FlaException("Rlm profile was null");
                var pa = new Prosumer(businessEntry.HouseGuid,
                    name,
                    HouseComponentType.BusinessWithLastgangLowVoltage,
                    businessEntry.Guid,
                    businessEntry.FinalIsn,
                    has.Guid,
                    has.ObjectID,
                    GenerationOrLoad.Load,
                    has.Trafokreis,
                    Name,
                    "Lastgang");
                var rlmprofile = _dbdto.MeasuredRlmProfiles.Single(x => x.Name == businessEntry.RlmProfileName);
                var profile = new Profile(rlmprofile.Name, rlmprofile.Profile.Values, rlmprofile.Profile.EnergyOrPower);
                if (profile.EnergyOrPower == EnergyOrPower.Power) {
                    profile = profile.ConvertFromPowerToEnergy();
                }

                var lowvoltageProfile = profile.ScaleToTargetSum(businessEntry.EffectiveEnergyDemand,
                    profile.Name,
                    out var _);
                if (lowvoltageProfile.Values.Any(x => double.IsNaN(x))) {
                    throw new FlaException("Profile contained NAN from provider " + ppdto.HouseComponent.Name);
                }

                pa.Profile = lowvoltageProfile;
                WriteLastgangToCsv(pa);
                return pa;
            }

            if (businessEntry.HouseComponentType == HouseComponentType.BusinessWithLastgangHighVoltage) {
                var has = _dbdto.Hausanschlusse.Single(x => x.Guid == businessEntry.HausAnschlussGuid);
                string name = businessEntry.RlmProfileName ?? throw new FlaException("Rlm profile was null");
                var pa = new Prosumer(businessEntry.HouseGuid,
                    name,
                    HouseComponentType.BusinessWithLastgangHighVoltage,
                    businessEntry.Guid,
                    businessEntry.FinalIsn,
                    has.Guid,
                    has.ObjectID,
                    GenerationOrLoad.Load,
                    has.Trafokreis,
                    Name,
                    "Lastgang HV");
                var rlmprofile = _dbdto.MeasuredRlmProfiles.Single(x => x.Name == businessEntry.RlmProfileName);
                var profile = new Profile(rlmprofile.Name, rlmprofile.Profile.Values, rlmprofile.Profile.EnergyOrPower);
                if (profile.EnergyOrPower == EnergyOrPower.Power) {
                    profile = profile.ConvertFromPowerToEnergy();
                }

                var highVoltageProfile = profile.ScaleToTargetSum(businessEntry.EffectiveEnergyDemand,
                    profile.Name,
                    out var _);
                if (highVoltageProfile.Values.Any(x => double.IsNaN(x))) {
                    throw new FlaException("Profile contained NAN from provider " + ppdto.HouseComponent.Name);
                }

                pa.Profile = highVoltageProfile;
                WriteLastgangToCsv(pa);
                return pa;
            }

            throw new FlaException("Unknown type");
        }

        private void WriteLastgangToCsv([NotNull] Prosumer pa)
        {
            if (!Slice.Equals(Constants.PresentSlice)) {
                return;
            }

            string key = pa.HausanschlussKey;
            string tmpkey = key;
            int cnt = 1;
            while (_usedKeys.Contains(tmpkey)) {
                tmpkey = key + "-" + cnt;
                cnt++;
            }

            key = tmpkey;
            _usedKeys.Add(key);
            string fn = FilenameHelpers.MakeAndRegisterFullFilenameStatic("Profile." + key + ".csv",
                Stage.ProfileGeneration,
                1000,
                "fn",
                Constants.PresentSlice,
                Services.RunningConfig,
                true);
            StreamWriter sw = new StreamWriter(fn);
            if (pa.Profile == null) {
                throw new FlaException("profile was null");
            }

            sw.WriteLine(pa.Name);
            sw.WriteLine(pa.HausanschlussKey);
            sw.WriteLine(pa.ProfileSourceName);
            sw.WriteLine(pa.Profile.GetCSVLine());
            sw.Close();
        }
    }
}
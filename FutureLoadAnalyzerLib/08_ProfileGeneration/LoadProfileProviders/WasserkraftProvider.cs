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
    public class WasserkraftProvider : BaseProvider, ILoadProfileProvider {
        [NotNull] private readonly DBDto _dbDto;

        public WasserkraftProvider([NotNull] ServiceRepository services, [NotNull] ScenarioSliceParameters slice, [NotNull] DBDto dbDto) : base(
            nameof(WasserkraftProvider),
            services,
            slice) =>
            _dbDto = dbDto;


        public bool IsCorrectProvider([NotNull] IHouseComponent houseComponent)
        {
            if (houseComponent.HouseComponentType == HouseComponentType.Kwkw) {
                return true;
            }

            return false;
        }

        public bool PrepareLoadProfileIfNeeded([NotNull] ProviderParameterDto parameters) => true;

        [CanBeNull]
        protected override Prosumer ProvidePrivateProfile([NotNull] ProviderParameterDto ppdto)
        {
            KleinWasserkraft wk = (KleinWasserkraft)ppdto.HouseComponent;
            if (wk.HouseComponentType != HouseComponentType.Kwkw) {
                throw new FlaException("Wrong type");
            }

            Hausanschluss ha = _dbDto.Hausanschlusse.Single(x => x.Guid == wk.HausAnschlussGuid);
            var rlmprofile = _dbDto.MeasuredRlmProfiles.Single(x => x.Name == wk.RlmProfileName);
            GenerationOrLoad gol = GenerationOrLoad.Generation;
            if (rlmprofile.SumElectricity > 0) {
                gol = GenerationOrLoad.Load;
            }
            var pa = new Prosumer(wk.HouseGuid,
                wk.Standort,
                HouseComponentType.BusinessNoLastgangLowVoltage,
                wk.SourceGuid,
                wk.FinalIsn,
                wk.HausAnschlussGuid,
                ha.ObjectID,
                gol,
                ha.Trafokreis,
                Name, "Wasserkraft Lastgang");
            var profile = new Profile(rlmprofile.Name, rlmprofile.Profile.Values, rlmprofile.Profile.EnergyOrPower);
            if (profile.EnergyOrPower == EnergyOrPower.Power) {
                profile = profile.ConvertFromPowerToEnergy();
            }

            if (profile.EnergySum() < 0) {
                profile = profile.MultiplyWith(-1, profile.Name);
            }

            if (profile.Values.Any(x => x < 0)) {
                throw new FlaException("Negative values in profile " + wk.Geschäftspartner + wk.Anlagennummer);
            }
            pa.Profile = profile;
            pa.SumElectricityPlanned = wk.EffectiveEnergyDemand;
            ppdto.HouseComponentResultObject.RlmFilename = wk.RlmProfileName;
            ppdto.HouseComponentResultObject.ProcessingStatus = "WKW Profile aus RLM von " + wk.RlmProfileName + " für " + wk.Anlagennummer;
            Debug("provided wasserkraft profile " + wk.Anlagennummer + " " + wk.Geschäftspartner);
            WriteLastgangToCsv(pa);
            return pa;
        }
        [NotNull] [ItemNotNull] private readonly HashSet<string> _usedKeys = new HashSet<string>();
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
                Stage.ProfileGeneration, 1001, "fn", Constants.PresentSlice, this.Services.RunningConfig, true);
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
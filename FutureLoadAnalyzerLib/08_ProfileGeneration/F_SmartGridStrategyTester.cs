using System.IO;
using System.Linq;
using Common;
using Common.Steps;
using Data.Database;
using Data.DataModel.ProfileImport;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling;
using FutureLoadAnalyzerLib.Tooling.Database;
using FutureLoadAnalyzerLib.Tooling.Steps;
using FutureLoadAnalyzerLib.Tooling.XlsDumper;
using JetBrains.Annotations;
using MessagePack;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration {
    // ReSharper disable once InconsistentNaming
    public class F_SmartGridStrategyTester : RunableForSingleSliceWithBenchmark {

        public F_SmartGridStrategyTester([NotNull] ServiceRepository services)
            : base(nameof(F_SmartGridStrategyTester), Stage.ProfileGeneration, 600, services, false, null)
        {
        }

        protected override void RunActualProcess(ScenarioSliceParameters slice)
        {
            string srcPath = FilenameHelpers.GetTargetDirectory(Stage.ProfileGeneration,
                500,
                nameof(E_ApplySmartGridToGeneratedProfiles),
                slice,
                Services.RunningConfig);
            string lz4ProfilePath = Path.Combine(srcPath, "addedProfile.lz4");
            byte[] arr = File.ReadAllBytes(lz4ProfilePath);
            var addedProfileraw = LZ4MessagePackSerializer.Deserialize<Profile>(arr);
            var pos = addedProfileraw.GetOnlyPositive("pos");
            var neg = addedProfileraw.GetOnlyNegative("neg");
            var addedProfile = pos.Add(neg, "added");

            var maxDailyGen = addedProfile.MakeDailyAverages().Values.Max() * 24 * 4;

            double storageSize = maxDailyGen * 2;
            MakeXlsForCurrentProfile(slice);

            MakeExampleWsForSmartgrid(slice, addedProfile, storageSize, 1);
            MakeExampleWsForSmartgrid(slice, addedProfile, storageSize, 0.5);
            MakeStorageSizeSheet(slice, maxDailyGen, addedProfile);
            MakeAbregelungWorksheet(slice, maxDailyGen, addedProfile);

        }

        private void MakeXlsForCurrentProfile([NotNull] ScenarioSliceParameters slice)
        {
            var dbRaw = Services.SqlConnectionPreparer.GetDatabaseConnection(Stage.Raw, Constants.PresentSlice);
            var bkwArr = dbRaw.Fetch<BkwProfile>();
            var bkwJProf = bkwArr[0];
            var bkwProf = new Profile(bkwJProf.Profile);
            var fnProfPresent = MakeAndRegisterFullFilename("PresentProfile.xlsx", slice);
            XlsxDumper.DumpProfilesToExcel(fnProfPresent,
                Constants.PresentSlice.DstYear,
                15,
                new ProfileWorksheetContent("bkw", "Last [kW]", 240, bkwProf)
            );
            SaveToPublicationDirectory(fnProfPresent, slice, "4.5");
        }

        private void MakeExampleWsForSmartgrid([NotNull] ScenarioSliceParameters slice, [NotNull] Profile addedProfile, double storageSize, double cappingFactor)
        {
            var minimzed3 = ProfileSmoothing.FindBestPowerReductionRatio(addedProfile, storageSize, out var _, out var reductionFactor3, cappingFactor);
            RowCollection rc = new RowCollection("TotalReduction", "Total Reduction");
            var rb = RowBuilder.Start("Reduction", reductionFactor3);
            rb.Add("BatterySize [GWh]", storageSize / 1000000);

            rc.Add(rb);
            var fnProf = MakeAndRegisterFullFilename("SmartGridTestProfiles_centralstorage_" + slice.DstYear +  "_capp_" + cappingFactor+ ".xlsx", slice);
            var manualParameters = ProfileSmoothing.UsePeakShaving(addedProfile,
                storageSize,
                true,
                out var storageProfile,
                reductionFactor3,
                out var temporaryTargetProfile,
                out var sum24HProfile,
                cappingFactor);
            var adjustedStorageProfile = storageProfile?.MultiplyWith(1 / 4000000.0, "Speicherfüllstand") ?? throw new FlaException();
            rb.Add("Energy", manualParameters.EnergySum());
            rb.Add("Min Power", manualParameters.MinPower());
            rb.Add("Max Power", manualParameters.MaxPower());
            XlsxDumper.DumpProfilesToExcel(fnProf,
                slice.DstYear,
                15,
                new RowWorksheetContent(rc),
                new ProfileWorksheetContent("raw", "Last [kW]", 240, addedProfile),
                new ProfileWorksheetContent("manualParameters", "Netto-Last [MW]", 240, manualParameters),
                new ProfileWorksheetContent("storageprofile", "Speicherfüllstand [GWh]", 240, adjustedStorageProfile),
                new ProfileWorksheetContent("minimized", "Netto-Last [MW]", 240, minimzed3),
                new ProfileWorksheetContent("temptarget", "Ziel-Füllstand Speicher [kWh]", 240, temporaryTargetProfile),
                new ProfileWorksheetContent("sum24h", "Tages-Energiemenge [GWh]", 240, sum24HProfile)
            );

            SaveToPublicationDirectory(fnProf, slice, "4.5");
        }

        private void MakeStorageSizeSheet([NotNull] ScenarioSliceParameters slice, double maxDailyGen,
                                          [NotNull] Profile addedProfile)
        {
            RowCollection rc = new RowCollection("effect", "Effekt");
            for (double i = 0; i < 10; i += 0.1) {
                double storageSize = maxDailyGen * i;
                var minimzed = ProfileSmoothing.FindBestPowerReductionRatio(addedProfile,
                    storageSize,
                    out var _,
                    out var reductionFactor,1);
                var minimzed2 = ProfileSmoothing.FindBestPowerReductionRatio(addedProfile,
                    storageSize,
                    out var _,
                    out var reductionFactor2, 0.5);
                double friendlySize = storageSize / Constants.GWhFactor;
                Info("Size: " + i + " " + friendlySize + " gwh, Reduction factor: " + reductionFactor);
                RowBuilder rb = RowBuilder.Start("Size", friendlySize);
                rb.Add("DaySize", i);
                rb.Add("ReductionFactor", reductionFactor);
                rb.Add("Max Power", minimzed.MaxPower() / 1000);
                rb.Add("Min Power", minimzed.MinPower() / 1000);
                rb.Add("Energy", minimzed.EnergySum() / 1000000);
                rb.Add("EnergyFromGrid", minimzed.GetOnlyPositive("pos").EnergySum() / 1000000);
                rb.Add("EnergyToGrid", minimzed.GetOnlyNegative("neg").EnergySum() / 1000000);
                rb.Add("ReductionFactor Curtailed", reductionFactor2);
                rb.Add("Max Power Curtailed", minimzed2.MaxPower() / 1000);
                rb.Add("Min Power Curtailed", minimzed2.MinPower() / 1000);
                rb.Add("Energy Curtailed", minimzed2.EnergySum() / 1000000);
                rb.Add("EnergyFromGrid Curtailed", minimzed2.GetOnlyPositive("pos").EnergySum() / 1000000);
                rb.Add("EnergyToGrid Curtailed", minimzed2.GetOnlyNegative("neg").EnergySum() / 1000000);
                rc.Add(rb);
            }

            var fnFactor = MakeAndRegisterFullFilename("BatterySizeImpact.xlsx", slice);
            XlsxDumper.WriteToXlsx(fnFactor, rc);
            SaveToPublicationDirectory(fnFactor,slice,"4.5");
        }

        private void MakeAbregelungWorksheet([NotNull] ScenarioSliceParameters slice, double maxDailyGen,
                                          [NotNull] Profile addedProfile)
        {
            RowCollection rc = new RowCollection("effect", "Effekt");
            for (double i = 0; i < 1; i += 0.01) {
                double storageSize = maxDailyGen * 2;
                var minimzed = ProfileSmoothing.FindBestPowerReductionRatio(addedProfile,
                    storageSize,
                    out var _,
                    out var reductionFactor, i);
                double friendlySize = storageSize / Constants.GWhFactor;
                Info("Size: " + i + " " + friendlySize + " gwh, Reduction factor: " + reductionFactor);
                RowBuilder rb = RowBuilder.Start("Size", friendlySize);
                rb.Add("storage", storageSize);
                rb.Add("ReductionFactor", reductionFactor);
                rb.Add("Capping", i);
                rb.Add("Max Power", minimzed.MaxPower() / 1000);
                rb.Add("Min Power", minimzed.MinPower() / 1000);
                rc.Add(rb);
            }

            var fnFactor = MakeAndRegisterFullFilename("CappingImpact.xlsx", slice);
            XlsxDumper.WriteToXlsx(fnFactor, rc);
            SaveToPublicationDirectory(fnFactor, slice, "4.5");
        }
        /*
        private void MakeLookAheadVariation([NotNull] ScenarioSliceParameters slice, Profile addedProfile, double maxDailyGen)
        {
            RowCollection maxVals = new RowCollection("Max");
            for (int lookAheadDistance = 1; lookAheadDistance < 40; lookAheadDistance++) {
                RowBuilder rb = RowBuilder.Start("LookAhead [h]/BufferSize [days]", lookAheadDistance * 6);
                for (int storageSize = 1; storageSize < 40; storageSize++) {
                    var buffered2 =
                        ProfileSmoothing.IntegrateStorageWiFloatingTargetValue(addedProfile, maxDailyGen * storageSize, "buffered", out var _, lookAheadDistance * 24);
                    rb.Add(storageSize.ToString(), buffered2.Values.Max());
                }

                maxVals.Add(rb);
            }

            var fnVariation = MakeAndRegisterFullFilename("LookAheadAnalysis.xlsx", slice);
            XlsxDumper.WriteToXlsx(fnVariation, maxVals);
        }*/


    }
}

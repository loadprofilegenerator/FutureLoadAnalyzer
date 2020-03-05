using System.IO;
using Common;
using Common.Config;
using Common.Logging;
using Common.Steps;
using Data.DataModel.Profiles;
using FutureLoadAnalyzerLib.Tooling.SAM;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib._08_ProfileGeneration.PVProfile {
    public class PVSystemSettings : BasicLoggable {
        private readonly int _idx;

        public PVSystemSettings(PVSystemKey key, float acPower, float dcAcRatio, [NotNull] ILogger logger, int idx) : base(logger,
            Stage.ProfileGeneration,
            nameof(PVSystemSettings))
        {
            PVSystemKey = key;
            AcPower = acPower;
            DcAcRatio = dcAcRatio;
            _idx = idx;
        }

        public float AcPower { get; }
        public float DcAcRatio { get; }
        public PVSystemKey PVSystemKey { get; }

        //[CanBeNull]public Thread MyThread { get; set; }
        [CanBeNull]
        public PVResults Result { get; set; }


        [NotNull]
        public Profile Run([NotNull] RunningConfig config)
        {
            var data = new Tooling.SAM.Data();
            var module = InitializeModule(data, PVSystemKey, AcPower, DcAcRatio, config);
            var result = ExecuteCalculation(module, data, AcPower, PVSystemKey);
            Info("Processing setting " + _idx);
            Result = result;
            Profile p = result.GetProfile();
            Profile pa;
            if ((p.Values.Count > 500000) & (p.Values.Count < 600000)) {
                //1 minute resolution, need to reduce.
                pa = p.ReduceTimeResolutionFrom1MinuteTo15Minutes();
            }
            else if (p.Values.Count > 35000 && p.Values.Count < 36000) {
                pa = p;
            }
            else {
                throw new FlaException("Unknown time resolution");
            }

            return pa;
        }

        [NotNull]
        private PVResults ExecuteCalculation([NotNull] Module module, [NotNull] Tooling.SAM.Data data, double powerinKw, PVSystemKey key)
        {
            if (!module.Exec(data)) {
                var idx = 0;
                while (module.Log(idx, out string msg, out int type, out float time)) {
                    var stype = "NOTICE";
                    if (type == API.WARNING) {
                        stype = "WARNING";
                    }
                    else if (type == API.ERROR) {
                        stype = "ERROR";
                    }

                    Info("[" + stype + " at time : " + time + "]: " + msg + " for azimut " + key.Azimut + " tilt: " + key.Tilt + " power " +
                         powerinKw);
                    idx++;
                }

                throw new FlaException("something went wrong");
            }

            //data.GetNumber("annual_energy"),
            var r = new PVResults(data.GetNumber("capacity_factor"), data.GetNumber("kwh_per_kw"), data.GetArrayAsList("ac"), powerinKw, key);
            return r;
        }

        [NotNull]
        private static Module InitializeModule([NotNull] Tooling.SAM.Data data,
                                               PVSystemKey key,
                                               float powerinKw,
                                               float dcAcRatio,
                                               [NotNull] RunningConfig config)
        {
            data.SetString("solar_resource_file", Path.Combine(config.Directories.SamDirectory, "2050_V1_1.csv"));
            // data.SetString( "solar_resource_file", "C:/Users/Pan2/Downloads/weather/tmy_era_47.056_7.585_2005_2014.epw" );
            data.SetNumber("system_capacity", powerinKw);
            data.SetNumber("module_type", 0f);
            data.SetNumber("dc_ac_ratio", dcAcRatio);
            data.SetNumber("inv_eff", 96f);
            data.SetNumber("losses", 14.075660705566406f);
            data.SetNumber("array_type", 0f);
            data.SetNumber("tilt", key.Tilt);
            data.SetNumber("azimuth", key.Azimut);
            data.SetNumber("gcr", 0.40000000596046448f);
            data.SetNumber("adjust:constant", 0f);
            var module = new Module("pvwattsv5");
            if (null == module) {
                throw new FlaException("error: could not create 'pvwattsv5' module.");
            }

            return module;
        }
    }
}
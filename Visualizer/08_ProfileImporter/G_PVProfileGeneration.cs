using System;
using System.Collections.Generic;
using BurgdorfStatistics.Tooling;
using Common;
using Common.Steps;
using JetBrains.Annotations;
using Xunit;

namespace BurgdorfStatistics._08_ProfileImporter {
    public class PVSystemKeyTester {
        [Fact]
        public void TestPVSystemKey()
        {
            G_PVProfileGeneration.PVSystemKey pk1 = new G_PVProfileGeneration.PVSystemKey(10,10);
            G_PVProfileGeneration.PVSystemKey pk2 = new G_PVProfileGeneration.PVSystemKey(10, 10);
            Assert.Equal(pk1,pk2);
            Assert.True(pk1==pk2);
        }
    }
    // ReSharper disable once InconsistentNaming
    public class G_PVProfileGeneration : RunableWithBenchmark {
        public struct PVSystemKey:  IEquatable<PVSystemKey> {
            public override string ToString() => GetKey();

            public PVSystemKey(int azimut, int tilt)
            {
                Azimut =(azimut/5)*5;
                Tilt = (tilt / 5)*5;
                if (azimut == 360) {
                    throw new FlaException("Azimut 360 is not ok.");
                }

                if (azimut < 0) {
                    throw new FlaException("Azimut < 0 is not ok.");
                }
            }

            public bool Equals(PVSystemKey other) => Azimut.Equals(other.Azimut) && Tilt.Equals(other.Tilt);

            public override bool Equals(object obj) => obj is PVSystemKey other && Equals(other);
            public static bool operator ==(PVSystemKey pk1, PVSystemKey pk2)
            {
                return pk1.Equals(pk2);
            }

            public static bool operator !=(PVSystemKey pk1, PVSystemKey pk2)
            {
                return !pk1.Equals(pk2);
            }

            public override int GetHashCode()
            {
                unchecked {
                    return (Azimut.GetHashCode() * 397) ^ Tilt.GetHashCode();
                }
            }

            public int Azimut { get;  }
            public int Tilt { get;  }

            [NotNull]
            public string GetLine()
            {
                return Azimut + ";" + Tilt;
            }

            [NotNull]
            public string GetKey()
            {
                return Azimut + "###" + Tilt;
            }
        }
        /*
        private class PVSystemSettings
        {
            private readonly Logger _logger;
            private readonly int _idx;

            public PVSystemSettings(float tilt, float azimut, float acPower,
                                    float dcAcRatio, Logger logger, int idx )
            {
                if (azimut < 0) {
                    throw new FlaException("Azimut < 0");
                }
                Tilt = tilt;
                if(azimut > 359) {
                    throw new FlaException("Azimut > 355");
                }
                Azimut = azimut;
                AcPower = acPower;
                DcAcRatio = dcAcRatio;
                _logger = logger;
                _idx = idx;
            }

            public float Tilt { get; }
            public float Azimut { get; }
            public float AcPower { get; }
            public float DcAcRatio { get; }
            public Results Result { get; set; }

            public void Run( [NotNull] SaveableEntry<Profile> sa)
            {
                var data = new Tooling.SAM.Data();
                var module = InitializeModule(data, Tilt, Azimut, AcPower, DcAcRatio);
                Result = ExecuteCalculation(module, data, Tilt, Azimut,
                    _logger, AcPower);
                _logger.Info("Processing setting " + _idx);
                Profile p = Result.GetProfile();
                Profile pa;
                if (p.Values.Count > 500000 & p.Values.Count < 600000) {
                    //1 minute resolution, need to reduce.
                    pa = p.ReduceTimeResolutionFrom1MinuteTo15Minutes();
                }
                else if(p.Values.Count > 35000 && p.Values.Count < 36000) {
                    pa = p;
                }
                else { throw new FlaException("Unknown time resolution");}

                lock (sa) {
                    sa.AddRow(pa);
                    if (sa.RowEntries.Count > 10) {
                        sa.SaveDictionaryToDatabase();
                    }
                }
            }

            public Thread MyThread { get; set; }
        }*/
        public G_PVProfileGeneration([NotNull] ServiceRepository services)
            : base(nameof(G_PVProfileGeneration), Stage.ProfileImport, 700, services, false)
        {
            DevelopmentStatus.Add("use the correct weather year profile for the generation");
            DevelopmentStatus.Add("instead of commenting out, check if all angles & right weather file based on key. If not right, clear table and regenerate");
        }

        protected override void RunActualProcess()
        {
            /*
            var dbHouses = Services.SqlConnection.GetDatabaseConnection(Stage.Houses, Constants.PresentSlice);
            var pvPotentials = dbHouses.Database.Fetch<PVPotential>();
            Directory.SetCurrentDirectory(@"V:\Dropbox\BurgdorfStatistics\Sam");
            Info("SSC Version number = " + API.Version());
            Info("SSC bBuild Information = " + API.BuildInfo());
            Module.SetPrint(0);

            //ausrichtungen
            List<PVSystemKey> azimutAusrichtungen = new List<PVSystemKey>();
            var orientationsfilename = MakeAndRegisterFullFilename("PVSystemOrientations.csv", Name, "", Constants.PresentSlice);
            StreamWriter swOr = new StreamWriter(orientationsfilename);
            List<string> keys = new List<string>();
            foreach (PVPotential potential in pvPotentials)
            {
                int myazimut = (int)potential.Ausrichtung + 180;
                if (myazimut == 360)
                {
                    myazimut = 0;
                }
                PVSystemKey psk = new PVSystemKey(myazimut, (int)potential.Neigung);
                if (!azimutAusrichtungen.Contains(psk)) {
                    azimutAusrichtungen.Add(psk);
                    swOr.WriteLine(psk.GetLine());
                    string pskKey = psk.GetKey();
                    if (keys.Contains(pskKey)) {
                        throw new FlaException("Key already exists.");
                    }
                    keys.Add(pskKey);
                }
            }
            swOr.Close();
            Info("Found " + azimutAusrichtungen.Count + " unique system orientations");

            //calculate the pv systems
            var sets = new List<PVSystemSettings>();
            const float dcacratio = 1f;
            int idx = 0;
            foreach (var ausrichtungen in azimutAusrichtungen) {
                float acpower = 1;
                var s = new PVSystemSettings(ausrichtungen.Tilt, ausrichtungen.Azimut,
                    acpower, dcacratio, Services.Logger,idx++);
                sets.Add(s);
            }

            //make the calculations and save the results
            var dbPVProfiles = Services.SqlConnection.GetDatabaseConnection(Stage.ProfileGenerationPV, Constants.PresentSlice);
            //var results = ParallelExecute(sets);
            var sa = Profile.GetSaveableEntry(dbPVProfiles, TableType.PVGeneration);
            sa.MakeTableForListOfFields();
            ParallelExecute(sets, sa);
            sa.SaveDictionaryToDatabase();
            //string dstpath = @"c:\work\t.csv";
            //var dstsumspath = @"c:\work\sums.dcac." + dcacratio.ToString("F1") + ".csv";

            //save results*/

        }
        /*
        private static void ParallelExecute([NotNull] List<PVSystemSettings> sets, SaveableEntry<Profile> sa)
        {
            var activeThreads = new List<Thread>();
            foreach (var set in sets)
            {
                var t = new Thread(x=> set.Run(sa));
                t.Start();
                set.MyThread = t;
                activeThreads.Add(t);
                while (activeThreads.Count > 6)
                {
                    activeThreads[0].Join();
                    activeThreads.RemoveAt(0);
                }
            }

            //var results = new List<Results>();
            foreach (var set in sets)
            {
                set.MyThread.Join();
              //  results.Add(set.Result);
            }
        }*/
/*
        [NotNull]
        private static Module InitializeModule([NotNull] Tooling.SAM.Data data, float tilt, float azimut, float powerinKw, float dcAcRatio)
        {
            data.SetString("solar_resource_file", "V:/Dropbox/BurgdorfStatistics/Sam/2050_V1_1.csv");
            // data.SetString( "solar_resource_file", "C:/Users/Pan2/Downloads/weather/tmy_era_47.056_7.585_2005_2014.epw" );
            data.SetNumber("system_capacity", powerinKw);
            data.SetNumber("module_type", 0f);
            data.SetNumber("dc_ac_ratio", dcAcRatio);
            data.SetNumber("inv_eff", 96f);
            data.SetNumber("losses", 14.075660705566406f);
            data.SetNumber("array_type", 0f);
            data.SetNumber("tilt", tilt);
            data.SetNumber("azimuth", azimut);
            data.SetNumber("gcr", 0.40000000596046448f);
            data.SetNumber("adjust:constant", 0f);
            var module = new Module("pvwattsv5");
            if (null == module)
            {
                throw new Exception("error: could not create 'pvwattsv5' module.");
            }

            return module;
        }

        [NotNull]
        private static Results ExecuteCalculation([NotNull] Module module, [NotNull] Tooling.SAM.Data data, double tilt,
                                                  double azimut, Logger logger, double powerinKw)
        {
            if (!module.Exec(data))
            {
                var idx = 0;
                while (module.Log(idx, out string msg, out int type, out float time))
                {
                    var stype = "NOTICE";
                    if (type == API.WARNING)
                    {
                        stype = "WARNING";
                    }
                    else if (type == API.ERROR)
                    {
                        stype = "ERROR";
                    }
                    logger.Info("[" + stype + " at time : " + time + "]: " + msg + " for azimut " +azimut + " tilt: " + tilt + " power" + powerinKw);
                    idx++;
                }

                throw new Exception("something went wrong");
            }
            //data.GetNumber("annual_energy"),
            var r = new Results(
                data.GetNumber("capacity_factor"),
                data.GetNumber("kwh_per_kw"),
                data.GetArrayAsList("ac"), tilt, azimut, powerinKw);
            return r;
        }
        */
        private class Results {
            public Results( double capacityFactor, double kwhPerKW,
                           [NotNull] List<float> pvProfile, double tilt, double azimut, double maxPower)
            {
                //AnnualEnergy = annualEnergy;
                CapacityFactor = capacityFactor;
                KwhPerKW = kwhPerKW;
                PVProfile = pvProfile;
                Tilt = tilt;
                Azimut = azimut;
                MaxPower = maxPower;
            }
            //public double AnnualEnergy { get; }
            public double CapacityFactor { [UsedImplicitly] get; }

            public double KwhPerKW { [UsedImplicitly] get; }

            //public List<float> Tamb { get; set; }
            [NotNull]
            public List<float> PVProfile { [UsedImplicitly] get;  }
            public double Tilt { [UsedImplicitly] get;  }
            public double Azimut { [UsedImplicitly] get;  }

            public double MaxPower { [UsedImplicitly] get;  }
            /*
            [NotNull]
            public Profile GetProfile()
            {
                string key = new PVSystemKey((int)Azimut, (int)Tilt).GetKey();
                return new Profile(key, PVProfile.ConvertAll(x=>(double) x).ToList().AsReadOnly(),ProfileType.Power);
            }*/
        }
    }
}
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Common.Config {
    public class DirectoryConfig {
        public DirectoryConfig([NotNull] string baseProcessingDirectory,
                               [NotNull] string baseRawDirectory,
                               [NotNull] string baseUserSettingsDirectory,
                               [NotNull] string calcServerLpgDirectory,
                               [NotNull] string lpgReleaseDirectory,
                               [NotNull] string samDirectory,
                               [NotNull] string resultStorageDirectory,
                               [NotNull] string unitTestingDirectory,
                               [NotNull] string houseJobsDirectory)
        {
            BaseProcessingDirectory = baseProcessingDirectory;
            BaseRawDirectory = baseRawDirectory;
            BaseUserSettingsDirectory = baseUserSettingsDirectory;
            CalcServerLpgDirectory = calcServerLpgDirectory;
            LPGReleaseDirectory = lpgReleaseDirectory;
            SamDirectory = samDirectory;
            ResultStorageDirectory = resultStorageDirectory;
            UnitTestingDirectory = unitTestingDirectory;
            HouseJobsDirectory = houseJobsDirectory;
        }

        [NotNull]
        public string HouseJobsDirectory { get; set; }

        [Obsolete("Json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public DirectoryConfig()
        {
        }

        [NotNull]
        public string BaseProcessingDirectory { get; set; }

        [NotNull]
        public string BaseRawDirectory { get; set; }

        [NotNull]
        public string BaseUserSettingsDirectory { get; set; }

        [NotNull]
        public string CalcServerLpgDirectory { get; set; }

        [NotNull]
        public string LPGReleaseDirectory { get; set; }

        [NotNull]
        public string ResultStorageDirectory { get; set; }

        [NotNull]
        public string SamDirectory { get; set; }

        [NotNull]
        public string UnitTestingDirectory { get; set; }

        public void CheckInitalisation()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            Console.WriteLine("Checking directory config");
            Console.WriteLine(json);
            Console.WriteLine("finished directory config");
            var myPropertyInfos = GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in myPropertyInfos) {
                object val = propertyInfo.GetValue(this);
                if (val == null) {
                    throw new FlaException("The property " + propertyInfo.Name +
                                           " was null on the directories config object.");
                }
            }

            if (string.IsNullOrWhiteSpace(BaseProcessingDirectory)) {
                throw new FlaException("Base processing directory was not set");
            }

            if (string.IsNullOrWhiteSpace(BaseRawDirectory)) {
                throw new FlaException("Base raw directory was not set");
            }

            if (string.IsNullOrWhiteSpace(BaseUserSettingsDirectory)) {
                throw new FlaException("BaseUserSettingsDirectory directory was not set");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Common;
using Data.Database;
using Data.DataModel.Creation;
using Data.DataModel.Profiles;
using JetBrains.Annotations;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SQLite;

namespace Data.DataModel.Export {
    public enum SaveableEntryTableType {
        HouseLoad,
        HouseGeneration,
        PVGeneration,
        LPGProfile,
        Testing,
        EvProfile,
        SummedLoadsForAnalysis,
        SummedHouseProfiles,
        Smartgrid
    }

    public enum GenerationOrLoad {
        Load,
        Generation

    }


    /// <summary>
    ///     for storing a single household / device
    /// </summary>
    ///
    [MessagePackObject]
    public class Prosumer : BasicSaveable<Prosumer> {
        public Prosumer([JetBrains.Annotations.NotNull] string houseGuid,
                        [JetBrains.Annotations.NotNull] string name,
                        HouseComponentType houseComponentType,
                        [CanBeNull] string sourceGuid,
                        long isn,
                        [JetBrains.Annotations.NotNull] string hausanschlussGuid,
                        [JetBrains.Annotations.NotNull] string hausanschlussKey,
                        GenerationOrLoad generationOrLoad,
                        [JetBrains.Annotations.NotNull] string trafoKreis,
                        [JetBrains.Annotations.NotNull] string providerName,
                        [JetBrains.Annotations.NotNull] string profileSourceName)
        {
            HouseGuid = houseGuid;
            Name = name;
            HouseComponentType = houseComponentType;
            SourceGuid = sourceGuid;
            Isn = isn;
            HausanschlussGuid = hausanschlussGuid;
            HausanschlussKey = hausanschlussKey;
            GenerationOrLoad = generationOrLoad;

            TrafoKreis = trafoKreis;
            ProviderName = providerName;
            ProfileSourceName = profileSourceName;
        }

        [Obsolete("for json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public Prosumer()
        {
        }

        [Key(0)]
        public GenerationOrLoad GenerationOrLoad { get; set; }

        [JetBrains.Annotations.NotNull]
        [Key(3)]
        public string HausanschlussGuid { get; set; }

        [JetBrains.Annotations.NotNull]
        [Key(1)]
        public string HausanschlussKey { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [Key(11)]
        public HouseComponentType HouseComponentType { get; set; }

        [JetBrains.Annotations.NotNull]
        [Key(4)]
        public string HouseGuid { get; set; }

        [PrimaryKey]
        [Key(5)]
        public int ID { get; set; }

        [Key(2)]
        public long Isn { get; set; }

        [JetBrains.Annotations.NotNull]
        [Key(6)]
        public string Name { get; set; }

        [NPoco.Ignore]
        [Ignore]
        [CanBeNull]
        [Key(7)]
        public Profile Profile { get; set; }

        [JetBrains.Annotations.NotNull]
        [Key(13)]
        public string ProfileSourceName { get; set; }

        [Key(12)]
        [JetBrains.Annotations.NotNull]
        public string ProviderName { get; set; }

        [CanBeNull]
        [Key(8)]
        public string SourceGuid { get; set; }

        [IgnoreMember]
        public double SumElectricityFromProfile {
            get => Profile?.Values.Sum() / 4 ?? 0;
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        /// <summary>
        /// used only for creating standardlastprofile
        /// </summary>
        [Key(9)]
        public double SumElectricityPlanned { get; set; }

        [CanBeNull]
        [Key(10)]
        public string TrafoKreis { get; set; }

        [IgnoreMember]
        public int ValueCount {
            get => Profile?.Values.Count ?? 0;
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }

        [JetBrains.Annotations.NotNull]
        public string GetCSVLine()
        {
            if (Profile == null) {
                throw new Exception("Profile was null");
            }

            if (string.IsNullOrWhiteSpace(HausanschlussKey)) {
                throw new FlaException("Trying to save prosumer with empty hausanschlusskey");
            }

            return Isn + ";SM-Pros;MAXMEASUREDVALUE;" + HausanschlussKey + ";" + Profile.GetCSVLine();
        }


        [JetBrains.Annotations.NotNull]
        public Profile GetOrCreateNewProfile()
        {
            if (Profile != null) {
                if (Constants.MakeDummyProfilesOnly) {
                    if (Profile.Values.Count > 2) {
                        var vals = new List<double>();
                        var sum = Profile.EnergySum();
                        vals.Add(sum / 2);
                        vals.Add(sum / 2);
                        var p = new Profile(Profile.Name, vals.AsReadOnly(), EnergyOrPower.Energy);
                        Profile = p;
                    }
                }

                return Profile;
            }

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (HouseComponentType) {
                case HouseComponentType.Household:
                    return Profile.MakeConstantProfile(SumElectricityPlanned, Name, Profile.ProfileResolution.QuarterHour);
                case HouseComponentType.BusinessNoLastgangLowVoltage:
                    return Profile.MakeConstantProfile(SumElectricityPlanned, Name, Profile.ProfileResolution.QuarterHour);
                case HouseComponentType.HouseLoad:
                    return Profile.MakeConstantProfile(SumElectricityPlanned, Name, Profile.ProfileResolution.QuarterHour);
                default:
                    throw new FlaException("ProsumerType forgotten enum: " + HouseComponentType);
            }
        }

        protected override void SetAdditionalFieldsForRow([JetBrains.Annotations.NotNull] RowBuilder rb)
        {
            if (HouseGuid == null) {
                throw new Exception("Houseguid was null");
            }

            rb.Add("HouseGuid", HouseGuid).Add("Name", Name).Add("ID", ID).Add("ProsumerType", HouseComponentType).Add("SourceGuid", SourceGuid).Add("SumElectricityFromProfile", SumElectricityFromProfile)
                .Add("SumElectricityPlanned", SumElectricityPlanned).Add("ValueCount", ValueCount).Add("TrafoKreis", TrafoKreis).Add("ISN", Isn).Add("HausanschlussGuid", HausanschlussGuid)
                .Add("HausanschlussKey", HausanschlussKey).Add("ProfileSource", ProfileSourceName);
        }

        protected override void SetFieldListToSaveOtherThanMessagePack([JetBrains.Annotations.NotNull] Action<string, SqliteDataType> addField)
        {
                addField("ID", SqliteDataType.Integer);
                addField("HouseGuid", SqliteDataType.Text);
                addField("Name", SqliteDataType.Text);
                addField("ProsumerType", SqliteDataType.Integer);
                addField("SourceGuid", SqliteDataType.Text);
                addField("SumElectricityFromProfile", SqliteDataType.Double);
                addField("SumElectricityPlanned", SqliteDataType.Double);
                addField("ValueCount", SqliteDataType.Integer);
                addField("TrafoKreis", SqliteDataType.Text);
                addField("ISN", SqliteDataType.Integer);
                addField("HausanschlussKey", SqliteDataType.Text);
                addField("HausanschlussGuid", SqliteDataType.Text);
                addField("ProfileSource", SqliteDataType.Text);
        }
    }
}
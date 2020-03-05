using System;
using System.Diagnostics.CodeAnalysis;
using Common.Database;
using NPoco;
using SQLite;

namespace Data.DataModel {
    [TableName(nameof(PVPotential))]
    [Table(nameof(PVPotential))]
    public class PVPotential : IGuidProvider {
        public PVPotential([JetBrains.Annotations.NotNull] string houseGuid, [JetBrains.Annotations.NotNull] string guid)
        {
            HouseGuid = houseGuid;
            Guid = guid;
        }

        [Obsolete("For json only")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public PVPotential()
        {
        }

        public double Ausrichtung { get; set; }
        public double GesamtStrahlung { get; set; }
        [JetBrains.Annotations.NotNull]
        public string HouseGuid { get; set; }

        [SQLite.PrimaryKey]
        [AutoIncrement]
        public int ID { get; set; }
        [JetBrains.Annotations.NotNull]
        public string Guid { get; set; }
        public double MittlereStrahlung { get; set; }
        public double Neigung { get; set; }
        public double SonnendachBedarfHeizung { get; set; }
        public double SonnendachBedarfWarmwasser { get; set; }
        public double SonnendachStromErtrag { get; set; }
    }
}
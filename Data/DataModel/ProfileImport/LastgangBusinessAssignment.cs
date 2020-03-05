using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NPoco;
using SQLite;

namespace Data.DataModel.ProfileImport {
    [TableName(nameof(LastgangBusinessAssignment))]
    [Table(nameof(LastgangBusinessAssignment))]
    [NPoco.PrimaryKey(nameof(ID))]
    [SuppressMessage("ReSharper", "PublicMembersMustHaveComments")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class LastgangBusinessAssignment {
        [Obsolete("Only for json")]
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public LastgangBusinessAssignment()
        {
        }

        public LastgangBusinessAssignment([CanBeNull] string rlmFilename, [CanBeNull] string complexName,
                                          [CanBeNull] string businessName, [CanBeNull] string erzeugerID)
        {
            RlmFilename = rlmFilename;
            ComplexName = complexName;
            BusinessName = businessName;
            ErzeugerID = erzeugerID;
        }

        [CanBeNull]
        public string RlmFilename { get; set; }
        [CanBeNull]
        public string ComplexName { get; set; }
        [CanBeNull]
        public string BusinessName { get; set; }
        public int ID { get; set; }
        [CanBeNull]
        public string Standort { get; set; }
        [CanBeNull]
        public string ErzeugerID { get; set; }
    }
}
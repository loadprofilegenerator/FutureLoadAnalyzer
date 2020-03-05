#pragma warning disable CA1707 // Identifiers should not contain underscores
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Data.DataModel.Src {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
    [UsedImplicitly]
    public class LocalnetHausanschlussImport {
        public long u_obj_id_i { get; set; }
        public double hkoord { get; set; }
        public double vkoord { get; set; }

        [NotNull]
        public string u_objekt_f { get; set; }

        public int u_egid_ise { get; set; }

        [NotNull]
        public string u_strasse1 { get; set; }

        [NotNull]
        public string u_str_nr_i { get; set; }
    }
}
#pragma warning restore CA1707 // Identifiers should not contain underscores
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.SAM {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class API {
        // constants for return value of Info.VarType() (see sscapi.h)
        public const int INPUT = 1;
        public const int OUTPUT = 2;
        public const int INOUT = 3;


        // constants for out integer type in Module.Log() method (see sscapi.h)
        public const int NOTICE = 1;
        public const int WARNING = 2;
        public const int ERROR = 3;


        // constants for return value of Data.Query() and Info.DataType() (see sscapi.h)
        public const int INVALID = 0;
        public const int STRING = 1;
        public const int NUMBER = 2;
        public const int ARRAY = 3;
        public const int MATRIX = 4;
        public const int TABLE = 5;

        public static int Version() => NativeMethods.ssc_version();

        [CanBeNull]
        public static string BuildInfo()
        {
            var buildInfo = NativeMethods.ssc_build_info();
            return Marshal.PtrToStringAnsi(buildInfo);
        }
    }
}
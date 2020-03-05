using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.SAM {
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
#pragma warning disable CA1716 // Identifiers should not match keywords
    public class Module {
#pragma warning restore CA1716 // Identifiers should not match keywords
        private HandleRef m_mod;

        public static void SetPrint(int print)
        {
            NativeMethods.ssc_module_exec_set_print(print);
        }

        public Module([NotNull] string name) => m_mod = new HandleRef(this, NativeMethods.ssc_module_create(name));

        ~Module()
        {
            if (m_mod.Handle != IntPtr.Zero) {
                NativeMethods.ssc_module_free(m_mod);
            }
        }

        public bool IsOk() => m_mod.Handle != IntPtr.Zero;

        public HandleRef GetModuleHandle() => m_mod;

        public bool Exec([NotNull] Data data) => NativeMethods.ssc_module_exec(m_mod, data.GetDataHandle()) != 0;

        public bool Log(int idx, [CanBeNull] out string msg, out int type, out float time)
        {
            msg = "";
            var p = NativeMethods.ssc_module_log(m_mod, idx, out type, out time);
            if (IntPtr.Zero != p) {
                msg = Marshal.PtrToStringAnsi(p);
                return true;
            }

            return false;
        }
    }
}
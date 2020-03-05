using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.SAM {
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class PVSystemInfo {
        private HandleRef m_inf;
        [NotNull] private readonly Module m_mod;
        private int m_idx;

        public PVSystemInfo([NotNull] Module m)
        {
            m_mod = m;
            m_idx = 0;
        }

        public void Reset()
        {
            m_idx = 0;
        }

        public bool Get()
        {
            var p = NativeMethods.ssc_module_var_info(m_mod.GetModuleHandle(), m_idx);
            if (p == IntPtr.Zero) {
                Reset();
                return false;
            }

            m_inf = new HandleRef(this, p);
            m_idx++;
            return true;
        }

        [CanBeNull]
        public string Name()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return null;
            }

            var p = NativeMethods.ssc_info_name(m_inf);
            return Marshal.PtrToStringAnsi(p);
        }

        public int VarType()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return -1;
            }

            return NativeMethods.ssc_info_var_type(m_inf);
        }

        public int DataType()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return -1;
            }

            return NativeMethods.ssc_info_data_type(m_inf);
        }

        [CanBeNull]
        public string Label()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return null;
            }

            var p = NativeMethods.ssc_info_label(m_inf);
            return Marshal.PtrToStringAnsi(p);
        }

        [CanBeNull]
        public string Units()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return null;
            }

            var p = NativeMethods.ssc_info_units(m_inf);
            return Marshal.PtrToStringAnsi(p);
        }

        [CanBeNull]
        public string Meta()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return null;
            }

            var p = NativeMethods.ssc_info_meta(m_inf);
            return Marshal.PtrToStringAnsi(p);
        }

        [CanBeNull]
        public string Group()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return null;
            }

            var p = NativeMethods.ssc_info_group(m_inf);
            return Marshal.PtrToStringAnsi(p);
        }

        [CanBeNull]
        public string Required()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return null;
            }

            var p = NativeMethods.ssc_info_required(m_inf);
            return Marshal.PtrToStringAnsi(p);
        }

        [CanBeNull]
        public string Constraints()
        {
            if (m_inf.Handle == IntPtr.Zero) {
                return null;
            }

            var p = NativeMethods.ssc_info_constraints(m_inf);
            return Marshal.PtrToStringAnsi(p);
        }
    }
}
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace BurgdorfStatistics.Tooling.SAM {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Entry {
        private HandleRef m_entry;
        private int m_idx;

        public Entry() => m_idx = 0;

        public void Reset()
        {
            m_idx = 0;
        }

        public bool Get()
        {
            var p = sscapi.ssc_module_entry(m_idx);
            if (p == IntPtr.Zero) {
                Reset();
                return false;
            }

            m_entry = new HandleRef(this, p);
            m_idx++;
            return true;
        }

        [CanBeNull]
        public string Name()
        {
            if (m_entry.Handle != IntPtr.Zero) {
                var p = sscapi.ssc_entry_name(m_entry);
                return Marshal.PtrToStringAnsi(p);
            }

            return null;
        }

        [CanBeNull]
        public string Description()
        {
            if (m_entry.Handle != IntPtr.Zero) {
                var p = sscapi.ssc_entry_description(m_entry);
                return Marshal.PtrToStringAnsi(p);
            }

            return null;
        }

        public int Version()
        {
            if (m_entry.Handle != IntPtr.Zero) {
                return sscapi.ssc_entry_version(m_entry);
            }

            return -1;
        }
    }
}
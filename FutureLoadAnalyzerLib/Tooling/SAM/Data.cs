using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace FutureLoadAnalyzerLib.Tooling.SAM {
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    [SuppressMessage("ReSharper", "InlineOutVariableDeclaration")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
#pragma warning disable CA1724 // Type names should not match namespaces
    public class Data {
#pragma warning restore CA1724 // Type names should not match namespaces
        private HandleRef m_data;
        private readonly bool m_owned;


        public Data()
        {
            m_data = new HandleRef(this, NativeMethods.ssc_data_create());
            m_owned = true;
        }

        public Data(IntPtr dataRefNotOwned)
        {
            m_data = new HandleRef(this, dataRefNotOwned);
            m_owned = false;
        }

        ~Data()
        {
            if (m_owned && m_data.Handle != IntPtr.Zero) {
                NativeMethods.ssc_data_free(m_data);
            }
        }

        public void Clear()
        {
            NativeMethods.ssc_data_clear(m_data);
        }

        [CanBeNull]
        public string First()
        {
            var p = NativeMethods.ssc_data_first(m_data);
            if (p != IntPtr.Zero) {
                return Marshal.PtrToStringAnsi(p);
            }

            return null;
        }

        [CanBeNull]
        public string Next()
        {
            var p = NativeMethods.ssc_data_next(m_data);
            if (p != IntPtr.Zero) {
                return Marshal.PtrToStringAnsi(p);
            }

            return null;
        }

        public int Query([NotNull] string name) => NativeMethods.ssc_data_query(m_data, name);

        public void SetNumber([NotNull] string name, float value)
        {
            NativeMethods.ssc_data_set_number(m_data, name, value);
        }

        public float GetNumber([NotNull] string name)
        {
            NativeMethods.ssc_data_get_number(m_data, name, out var val);
            return val;
        }

        public void SetString([NotNull] string name, [NotNull] string value)
        {
            NativeMethods.ssc_data_set_string(m_data, name, value);
        }

        [CanBeNull]
        public string GetString([NotNull] string name)
        {
            var p = NativeMethods.ssc_data_get_string(m_data, name);
            return Marshal.PtrToStringAnsi(p);
        }

        public void SetArray([NotNull] string name, [NotNull] float[] data)
        {
            NativeMethods.ssc_data_set_array(m_data, name, data, data.Length);
        }

        public void SetArray([NotNull] string name, [NotNull] string fn, int len)
        {
            using (var sr = new StreamReader(fn)) {
                var Row = 0;
                var data = new float[len];
                while (!sr.EndOfStream && Row < len) {
                    // ReSharper disable once PossibleNullReferenceException
                    var line = sr.ReadLine().Split(',');
                    data[Row] = float.Parse(line[0]);
                    Row++;
                }

                NativeMethods.ssc_data_set_array(m_data, name, data, len);
            }
        }

        [NotNull]
        public List<float> GetArrayAsList([NotNull] string name)
        {
            var fls = GetArray(name);
            var fl = new List<float>(fls ?? throw new InvalidOperationException());
            return fl;
        }

        [CanBeNull]
        public float[] GetArray([NotNull] string name)
        {
            var res = NativeMethods.ssc_data_get_array(m_data, name, out var len);
            float[] arr = null;
            if (len > 0) {
                arr = new float[len];
                Marshal.Copy(res, arr, 0, len);
            }

            return arr;
        }

#pragma warning disable RINUL // Parameter is missing item nullability annotation.
        public void SetMatrix([NotNull] string name, [NotNull] float[,] mat)
#pragma warning restore RINUL // Parameter is missing item nullability annotation.
        {
            var nRows = mat.GetLength(0);
            var nCols = mat.GetLength(1);
            NativeMethods.ssc_data_set_matrix(m_data, name, mat, nRows, nCols);
        }

        public void SetMatrix([NotNull] string name, [NotNull] string fn, int nr, int nc)
        {
            using (var sr = new StreamReader(fn)) {
                var row = 0;
                var mat = new float[nr, nc];
                while (!sr.EndOfStream && row < nr) {
                    // ReSharper disable once PossibleNullReferenceException
                    var line = sr.ReadLine().Split(',');
                    for (var ic = 0; ic < line.Length && ic < nc; ic++) {
                        mat[row, ic] = float.Parse(line[ic]);
                    }

                    row++;
                }

                NativeMethods.ssc_data_set_matrix(m_data, name, mat, nr, nc);
            }
        }

        [CanBeNull]
#pragma warning disable RINUL // Method is missing item nullability annotation.
        public float[,] GetMatrix([NotNull] string name)
#pragma warning restore RINUL // Method is missing item nullability annotation.
        {
            var res = NativeMethods.ssc_data_get_matrix(m_data, name, out var nRows, out var nCols);
            if (nRows * nCols > 0) {
                var sscMat = new float[nRows * nCols];
                Marshal.Copy(res, sscMat, 0, nRows * nCols);
                var mat = new float[nRows, nCols];
                for (var i = 0; i < nRows; i++) {
                    for (var j = 0; j < nCols; j++) {
                        mat[i, j] = sscMat[i * nCols + j];
                    }
                }

                return mat;
            }

            return null;
        }

        public void SetTable([NotNull] string name, [NotNull] Data table)
        {
            NativeMethods.ssc_data_set_table(m_data, name, table.GetDataHandle());
        }

        [CanBeNull]
        public Data GetTable([NotNull] string name)
        {
            var p = NativeMethods.ssc_data_get_table(m_data, name);
            if (IntPtr.Zero == p) {
                return null;
            }

            return new Data(p);
        }

        public HandleRef GetDataHandle() => m_data;
    }
}
#pragma warning disable IDE1006 // Naming Styles
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace BurgdorfStatistics.Tooling.SAM {
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class sscapi {
        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_version")]
        public static extern int ssc_version32();

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_version")]
        public static extern int ssc_version64();

        public static int ssc_version() => IntPtr.Size == 8 ? ssc_version64() : ssc_version32();

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_build_info")]
        public static extern IntPtr ssc_build_info32();

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_build_info")]
        public static extern IntPtr ssc_build_info64();

        public static IntPtr ssc_build_info() => IntPtr.Size == 8 ? ssc_build_info64() : ssc_build_info32();

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_create")]
        public static extern IntPtr ssc_data_create32();

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_create")]
        public static extern IntPtr ssc_data_create64();

        public static IntPtr ssc_data_create() => IntPtr.Size == 8 ? ssc_data_create64() : ssc_data_create32();

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_free")]
        public static extern void ssc_data_free32(HandleRef cxtData);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_free")]
        public static extern void ssc_data_free64(HandleRef cxtData);

        public static void ssc_data_free(HandleRef cxtData)
        {
            if (IntPtr.Size == 8) {
                ssc_data_free64(cxtData);
            }
            else {
                ssc_data_free32(cxtData);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_clear")]
        public static extern void ssc_data_clear32(HandleRef cxtData);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_clear")]
        public static extern void ssc_data_clear64(HandleRef cxtData);

        public static void ssc_data_clear(HandleRef cxtData)
        {
            if (IntPtr.Size == 8) {
                ssc_data_clear64(cxtData);
            }
            else {
                ssc_data_clear32(cxtData);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_unassign")]
        public static extern void ssc_data_unassign32(HandleRef cxtData, [NotNull] string variableName);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_unassign")]
        public static extern void ssc_data_unassign64(HandleRef cxtData, [NotNull] string variableName);

        public static void ssc_data_unassign(HandleRef cxtData, [NotNull] string variableName)
        {
            if (IntPtr.Size == 8) {
                ssc_data_unassign64(cxtData, variableName);
            }
            else {
                ssc_data_unassign32(cxtData, variableName);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_query")]
        public static extern int ssc_data_query32(HandleRef cxtData, [NotNull] string variableName);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_query")]
        public static extern int ssc_data_query64(HandleRef cxtData, [NotNull] string variableName);

        public static int ssc_data_query(HandleRef cxtData, [NotNull] string variableName) => IntPtr.Size == 8 ? ssc_data_query64(cxtData, variableName) : ssc_data_query32(cxtData, variableName);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_first")]
        public static extern IntPtr ssc_data_first32(HandleRef cxtData);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_first")]
        public static extern IntPtr ssc_data_first64(HandleRef cxtData);

        public static IntPtr ssc_data_first(HandleRef cxtData) => IntPtr.Size == 8 ? ssc_data_first64(cxtData) : ssc_data_first32(cxtData);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_next")]
        public static extern IntPtr ssc_data_next32(HandleRef cxtData);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_next")]

        public static extern IntPtr ssc_data_next64(HandleRef cxtData);


        public static IntPtr ssc_data_next(HandleRef cxtData) => IntPtr.Size == 8 ? ssc_data_next64(cxtData) : ssc_data_next32(cxtData);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_string")]
        public static extern void ssc_data_set_string32(HandleRef cxtData, [NotNull] string name, [NotNull] string value);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_string")]
        public static extern void ssc_data_set_string64(HandleRef cxtData, [NotNull] string name, [NotNull] string value);

        public static void ssc_data_set_string(HandleRef cxtData, [NotNull] string name, [NotNull] string value)
        {
            if (IntPtr.Size == 8) {
                ssc_data_set_string64(cxtData, name, value);
            }
            else {
                ssc_data_set_string32(cxtData, name, value);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_number")]
        public static extern void ssc_data_set_number32(HandleRef cxtData, [NotNull] string name, float value);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_number")]
        public static extern void ssc_data_set_number64(HandleRef cxtData, [NotNull] string name, float value);

        public static void ssc_data_set_number(HandleRef cxtData, [NotNull] string name, float value)
        {
            if (IntPtr.Size == 8) {
                ssc_data_set_number64(cxtData, name, value);
            }
            else {
                ssc_data_set_number32(cxtData, name, value);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_array")]
        public static extern void ssc_data_set_array32(HandleRef cxtData, [NotNull] string name, [In] [MarshalAs(UnmanagedType.LPArray)] [NotNull]
                                                       float[] array, int length);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_array")]
        public static extern void ssc_data_set_array64(HandleRef cxtData, [NotNull] string name, [In] [MarshalAs(UnmanagedType.LPArray)] [NotNull]
                                                       float[] array, int length);

        public static void ssc_data_set_array(HandleRef cxtData, [NotNull] string name, [In] [MarshalAs(UnmanagedType.LPArray)] [NotNull]
                                              float[] array, int length)
        {
            if (IntPtr.Size == 8) {
                ssc_data_set_array64(cxtData, name, array, length);
            }
            else {
                ssc_data_set_array32(cxtData, name, array, length);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_matrix")]
        public static extern void ssc_data_set_matrix32(HandleRef cxtData, [NotNull] string name, [In] [MarshalAs(UnmanagedType.LPArray)] [NotNull]
#pragma warning disable RINUL // Parameter is missing item nullability annotation.
                                                        float[,] matrix, int nRows, int nCols);
#pragma warning restore RINUL // Parameter is missing item nullability annotation.

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_matrix")]
        public static extern void ssc_data_set_matrix64(HandleRef cxtData, [NotNull] string name, [In] [MarshalAs(UnmanagedType.LPArray)] [NotNull]
#pragma warning disable RINUL // Parameter is missing item nullability annotation.
                                                        float[,] matrix, int nRows, int nCols);
#pragma warning restore RINUL // Parameter is missing item nullability annotation.

        public static void ssc_data_set_matrix(HandleRef cxtData, [NotNull] string name, [In] [MarshalAs(UnmanagedType.LPArray)] [NotNull]
#pragma warning disable RINUL // Parameter is missing item nullability annotation.
                                               float[,] matrix, int nRows, int nCols)
#pragma warning restore RINUL // Parameter is missing item nullability annotation.
        {
            if (IntPtr.Size == 8) {
                ssc_data_set_matrix64(cxtData, name, matrix, nRows, nCols);
            }
            else {
                ssc_data_set_matrix32(cxtData, name, matrix, nRows, nCols);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_table")]
        public static extern void ssc_data_set_table32(HandleRef cxtData, [NotNull] string name, HandleRef cxtTable);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_set_table")]
        public static extern void ssc_data_set_table64(HandleRef cxtData, [NotNull] string name, HandleRef cxtTable);

        public static void ssc_data_set_table(HandleRef cxtData, [NotNull] string name, HandleRef cxtTable)
        {
            if (IntPtr.Size == 8) {
                ssc_data_set_table64(cxtData, name, cxtTable);
            }
            else {
                ssc_data_set_table32(cxtData, name, cxtTable);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_string")]
        public static extern IntPtr ssc_data_get_string32(HandleRef cxtData, [NotNull] string name);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_string")]
        public static extern IntPtr ssc_data_get_string64(HandleRef cxtData, [NotNull] string name);

        public static IntPtr ssc_data_get_string(HandleRef cxtData, [NotNull] string name) => IntPtr.Size == 8 ? ssc_data_get_string64(cxtData, name) : ssc_data_get_string32(cxtData, name);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_number")]
        public static extern int ssc_data_get_number32(HandleRef cxtData, [NotNull] string name, out float number);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_number")]
        public static extern int ssc_data_get_number64(HandleRef cxtData, [NotNull] string name, out float number);

        public static int ssc_data_get_number(HandleRef cxtData, [NotNull] string name, out float number) =>
            IntPtr.Size == 8 ? ssc_data_get_number64(cxtData, name, out number) : ssc_data_get_number32(cxtData, name, out number);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_array")]
        public static extern IntPtr ssc_data_get_array32(HandleRef cxtData, [NotNull] string name, out int len);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_array")]
        public static extern IntPtr ssc_data_get_array64(HandleRef cxtData, [NotNull] string name, out int len);

        public static IntPtr ssc_data_get_array(HandleRef cxtData, [NotNull] string name, out int len) =>
            IntPtr.Size == 8 ? ssc_data_get_array64(cxtData, name, out len) : ssc_data_get_array32(cxtData, name, out len);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_matrix")]
        public static extern IntPtr ssc_data_get_matrix32(HandleRef cxtData, [NotNull] string name, out int nRows, out int nCols);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_matrix")]
        public static extern IntPtr ssc_data_get_matrix64(HandleRef cxtData, [NotNull] string name, out int nRows, out int nCols);

        public static IntPtr ssc_data_get_matrix(HandleRef cxtData, [NotNull] string name, out int nRows, out int nCols) =>
            IntPtr.Size == 8 ? ssc_data_get_matrix64(cxtData, name, out nRows, out nCols) : ssc_data_get_matrix32(cxtData, name, out nRows, out nCols);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_table")]
        public static extern IntPtr ssc_data_get_table32(HandleRef cxtData, [NotNull] string name);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_data_get_table")]
        public static extern IntPtr ssc_data_get_table64(HandleRef cxtData, [NotNull] string name);

        public static IntPtr ssc_data_get_table(HandleRef cxtData, [NotNull] string name) => IntPtr.Size == 8 ? ssc_data_get_table64(cxtData, name) : ssc_data_get_table32(cxtData, name);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_entry")]
        public static extern IntPtr ssc_module_entry32(int moduleIndex);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_entry")]
        public static extern IntPtr ssc_module_entry64(int moduleIndex);

        public static IntPtr ssc_module_entry(int moduleIndex) => IntPtr.Size == 8 ? ssc_module_entry64(moduleIndex) : ssc_module_entry32(moduleIndex);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_entry_name")]
        public static extern IntPtr ssc_entry_name32(HandleRef cxtEntry);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_entry_name")]
        public static extern IntPtr ssc_entry_name64(HandleRef cxtEntry);

        public static IntPtr ssc_entry_name(HandleRef cxtEntry) => IntPtr.Size == 8 ? ssc_entry_name64(cxtEntry) : ssc_entry_name32(cxtEntry);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_entry_description")]
        public static extern IntPtr ssc_entry_description32(HandleRef cxtEntry);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_entry_description")]
        public static extern IntPtr ssc_entry_description64(HandleRef cxtEntry);

        public static IntPtr ssc_entry_description(HandleRef cxtEntry) => IntPtr.Size == 8 ? ssc_entry_description64(cxtEntry) : ssc_entry_description32(cxtEntry);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_entry_version")]
        public static extern int ssc_entry_version32(HandleRef cxtEntry);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_entry_version")]
        public static extern int ssc_entry_version64(HandleRef cxtEntry);

        public static int ssc_entry_version(HandleRef cxtEntry) => IntPtr.Size == 8 ? ssc_entry_version64(cxtEntry) : ssc_entry_version32(cxtEntry);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_create")]
        public static extern IntPtr ssc_module_create32([NotNull] string moduleName);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_create")]
        public static extern IntPtr ssc_module_create64([NotNull] string moduleName);

        public static IntPtr ssc_module_create([NotNull] string moduleName) => IntPtr.Size == 8 ? ssc_module_create64(moduleName) : ssc_module_create32(moduleName);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_free")]
        public static extern void ssc_module_free32(HandleRef cxtModule);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_free")]
        public static extern void ssc_module_free64(HandleRef cxtModule);

        public static void ssc_module_free(HandleRef cxtModule)
        {
            if (IntPtr.Size == 8) {
                ssc_module_free64(cxtModule);
            }
            else {
                ssc_module_free32(cxtModule);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_var_info")]
        public static extern IntPtr ssc_module_var_info32(HandleRef cxtModule, int index);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_var_info")]
        public static extern IntPtr ssc_module_var_info64(HandleRef cxtModule, int index);

        public static IntPtr ssc_module_var_info(HandleRef cxtModule, int index) => IntPtr.Size == 8 ? ssc_module_var_info64(cxtModule, index) : ssc_module_var_info32(cxtModule, index);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_var_type")]
        public static extern int ssc_info_var_type32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_var_type")]
        public static extern int ssc_info_var_type64(HandleRef cxtInfo);

        public static int ssc_info_var_type(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_var_type64(cxtInfo) : ssc_info_var_type32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_data_type")]
        public static extern int ssc_info_data_type32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_data_type")]
        public static extern int ssc_info_data_type64(HandleRef cxtInfo);

        public static int ssc_info_data_type(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_data_type64(cxtInfo) : ssc_info_data_type32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_name")]
        public static extern IntPtr ssc_info_name32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_name")]
        public static extern IntPtr ssc_info_name64(HandleRef cxtInfo);

        public static IntPtr ssc_info_name(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_name64(cxtInfo) : ssc_info_name32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_label")]
        public static extern IntPtr ssc_info_label32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_label")]
        public static extern IntPtr ssc_info_label64(HandleRef cxtInfo);

        public static IntPtr ssc_info_label(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_label64(cxtInfo) : ssc_info_label32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_units")]
        public static extern IntPtr ssc_info_units32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_units")]
        public static extern IntPtr ssc_info_units64(HandleRef cxtInfo);

        public static IntPtr ssc_info_units(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_units64(cxtInfo) : ssc_info_units32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_meta")]
        public static extern IntPtr ssc_info_meta32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_meta")]
        public static extern IntPtr ssc_info_meta64(HandleRef cxtInfo);

        public static IntPtr ssc_info_meta(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_meta64(cxtInfo) : ssc_info_meta32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_group")]
        public static extern IntPtr ssc_info_group32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_group")]
        public static extern IntPtr ssc_info_group64(HandleRef cxtInfo);

        public static IntPtr ssc_info_group(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_group64(cxtInfo) : ssc_info_group32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_required")]
        public static extern IntPtr ssc_info_required32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_required")]
        public static extern IntPtr ssc_info_required64(HandleRef cxtInfo);

        public static IntPtr ssc_info_required(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_required64(cxtInfo) : ssc_info_required32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_constraints")]
        public static extern IntPtr ssc_info_constraints32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_constraints")]
        public static extern IntPtr ssc_info_constraints64(HandleRef cxtInfo);

        public static IntPtr ssc_info_constraints(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_constraints64(cxtInfo) : ssc_info_constraints32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_uihint")]
        public static extern IntPtr ssc_info_uihint32(HandleRef cxtInfo);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_info_uihint")]
        public static extern IntPtr ssc_info_uihint64(HandleRef cxtInfo);

        public static IntPtr ssc_info_uihint(HandleRef cxtInfo) => IntPtr.Size == 8 ? ssc_info_uihint64(cxtInfo) : ssc_info_units32(cxtInfo);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec_simple")]
        public static extern int ssc_module_exec_simple32([NotNull] string moduleName, HandleRef cxtData);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec_simple")]
        public static extern int ssc_module_exec_simple64([NotNull] string moduleName, HandleRef cxtData);

        public static int ssc_module_exec_simple([NotNull] string moduleName, HandleRef cxtData) =>
            IntPtr.Size == 8 ? ssc_module_exec_simple64(moduleName, cxtData) : ssc_module_exec_simple32(moduleName, cxtData);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec_set_print")]
        public static extern void ssc_module_exec_set_print32(int print);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec_set_print")]
        public static extern void ssc_module_exec_set_print64(int print);

        public static void ssc_module_exec_set_print(int print)
        {
            if (IntPtr.Size == 8) {
                ssc_module_exec_set_print64(print);
            }
            else {
                ssc_module_exec_set_print32(print);
            }
        }

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec_simple_nothread")]
        public static extern IntPtr ssc_module_exec_simple_nothread32([NotNull] string moduleName, HandleRef cxtData);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec_simple_nothread")]
        public static extern IntPtr ssc_module_exec_simple_nothread64([NotNull] string moduleName, HandleRef cxtData);

        public static IntPtr ssc_module_exec_simple_nothread([NotNull] string moduleName, HandleRef cxtData) =>
            IntPtr.Size == 8 ? ssc_module_exec_simple_nothread64(moduleName, cxtData) : ssc_module_exec_simple_nothread32(moduleName, cxtData);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec")]
        public static extern int ssc_module_exec32(HandleRef cxtModule, HandleRef cxtData);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec")]
        public static extern int ssc_module_exec64(HandleRef cxtModule, HandleRef cxtData);

        public static int ssc_module_exec(HandleRef cxtModule, HandleRef cxtData) => IntPtr.Size == 8 ? ssc_module_exec64(cxtModule, cxtData) : ssc_module_exec32(cxtModule, cxtData);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec_with_handler")]
        public static extern int ssc_module_exec_with_handler32(HandleRef cxtModule, HandleRef cxtData, HandleRef cxtHandler, HandleRef cxtUserData);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_exec_with_handler")]
        public static extern int ssc_module_exec_with_handler64(HandleRef cxtModule, HandleRef cxtData, HandleRef cxtHandler, HandleRef cxtUserData);

        public static int ssc_module_exec_with_handler(HandleRef cxtModule, HandleRef cxtData, HandleRef cxtHandler, HandleRef cxtUserData) => IntPtr.Size == 8
            ? ssc_module_exec_with_handler64(cxtModule, cxtData, cxtHandler, cxtUserData)
            : ssc_module_exec_with_handler32(cxtModule, cxtData, cxtHandler, cxtUserData);

        [DllImport("ssc32.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_log")]
        public static extern IntPtr ssc_module_log32(HandleRef cxtModule, int index, out int messageType, out float time);

        [DllImport("ssc64.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ssc_module_log")]
        public static extern IntPtr ssc_module_log64(HandleRef cxtModule, int index, out int messageType, out float time);

        public static IntPtr ssc_module_log(HandleRef cxtModule, int index, out int messageType, out float time) =>
            IntPtr.Size == 8 ? ssc_module_log64(cxtModule, index, out messageType, out time) : ssc_module_log32(cxtModule, index, out messageType, out time);
    }
}
#pragma warning restore IDE1006 // Naming Styles
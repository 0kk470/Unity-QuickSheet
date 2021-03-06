﻿///////////////////////////////////////////////////////////////////////////////
///
/// ExcelQuery.cs
///
/// (c)2014 Kim, Hyoun Woo
///
///////////////////////////////////////////////////////////////////////////////
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using System.Linq;
using System.ComponentModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Text.RegularExpressions;
using System.Text;

namespace UnityQuickSheet
{
    /// <summary>
    /// Query each of cell data from the given excel sheet and deserialize it to the ScriptableObject's data array.
    /// </summary>
    public class ExcelQuery
    {
        private readonly IWorkbook workbook = null;
        private readonly ISheet sheet = null;
        private string filepath = string.Empty;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ExcelQuery(string path, string sheetName = "")
        {
            try
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    string extension = GetSuffix(path);

                    if (extension == "xls")
                        workbook = new HSSFWorkbook(fileStream);
                    else if (extension == "xlsx")
                    {
                    #if UNITY_EDITOR_OSX
                        throw new Exception("xlsx is not supported on OSX.");
                    #else
                        workbook = new XSSFWorkbook(fileStream);
                    #endif
                    }
                    else
                    {
                        throw new Exception("Wrong file.");
                    }

                    //NOTE: An empty sheetName can be available. Nothing to do with an empty sheetname.
                    if (!string.IsNullOrEmpty(sheetName))
                    {
                        sheet = workbook.GetSheet(sheetName);
                        if (sheet == null)
                            Debug.LogErrorFormat("Cannot find sheet '{0}'.", sheetName);
                    }

                    this.filepath = path;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        /// <summary>
        /// Determine whether the excel file is successfully read in or not.
        /// </summary>
        public bool IsValid()
        {
            if (this.workbook != null && this.sheet != null)
                return true;

            return false;
        }

        /// <summary>
        /// Retrieves file extension only from the given file path.
        /// </summary>
        static string GetSuffix(string path)
        {
            string ext = Path.GetExtension(path);
            string[] arg = ext.Split(new char[] { '.' });
            return arg[1];
        }

        string GetHeaderColumnName(int cellnum)
        {
            ICell headerCell = sheet.GetRow(0).GetCell(cellnum);
            if (headerCell != null)
                return headerCell.StringCellValue;
            return string.Empty;
        }

        /// <summary>
        /// Deserialize all the cell of the given sheet.
        ///
        /// NOTE:
        ///     The first row of a sheet is header column which is not the actual value
        ///     so it skips when it deserializes.
        /// </summary>
        public List<T> Deserialize<T>(int start = 1)
        {
            var t = typeof(T);
            PropertyInfo[] p = t.GetProperties();

            var result = new List<T>();

            var comments = new List<string>();

            int current = 0;
            foreach (IRow row in sheet)
            {
                if (current < start)
                {
                    current++; // skip header column.
                    for(int i = 0;i < row.Cells.Count; i++) //Deserialize Comment
                    {
                        var cell = row.Cells[i];
                        if (cell.CellComment != null)
                            comments.Add(cell.CellComment.String.ToString());
                        else
                            comments.Add(string.Empty);
                    }
                    continue;
                }

                var item = (T)Activator.CreateInstance(t);
                for (var i = 0; i < p.Length; i++)
                {
                    ICell cell = row.GetCell(i);

                    if (cell == null)  // skip empty cell
                        continue;

                    var property = p[i];
                    if (property.CanWrite)
                    {
                        try
                        {
                            var comment = comments[i]; //批注内容

                            if(comment.Contains("Bitwise") || comment.Contains("位组合"))
                            {
                                uint value = ConverBitwise(cell, comment);
                                property.SetValue(item, value, null);
                            }
                            else
                            {
                                var value = ConvertFrom(cell, property.PropertyType);
                                property.SetValue(item, value, null);
                            }
                        }
                        catch (Exception e)
                        {
                            string pos = string.Format("Row[{0}], Cell[{1}]", (current).ToString(), GetHeaderColumnName(i));
                            Debug.LogError(string.Format("Excel File {0} Deserialize Exception: {1} at {2}", this.filepath, e.Message, pos));
                        }
                    }
                }

                result.Add(item);

                current++;
            }

            return result;
        }

        /// <summary>
        /// Retrieves all sheet names.
        /// </summary>
        public string[] GetSheetNames()
        {
            List<string> sheetList = new List<string>();
            if (this.workbook != null)
            {
                int numSheets = this.workbook.NumberOfSheets;
                for (int i = 0; i < numSheets; i++)
                {
                    sheetList.Add(this.workbook.GetSheetName(i));
                }
            }
            else
                Debug.LogError("Workbook is null. Did you forget to import excel file first?");

            return (sheetList.Count > 0) ? sheetList.ToArray() : null;
        }

        /// <summary>
        /// Retrieves all first columns(aka. header column) which are needed to determine each type of a cell.
        /// </summary>
        public string[] GetTitle(int start, ref string error)
        {
            if (sheet == null)
            {
                error = @"Sheet is null";
                return null;
            }

            List<string> result = new List<string>();

            IRow title = sheet.GetRow(start);
            if (title != null)
            {
                for (int i = 0; i < title.LastCellNum; i++)
                {
                    var cell = title.GetCell(i);
                    if (cell == null)
                    {
                        // null or empty column is found. Note column index starts from 0.
                        Debug.LogWarningFormat("Null or empty column is found at {0}.\n", i);
                        continue;
                    }
                    string value = cell.StringCellValue;
                    if (string.IsNullOrEmpty(value))
                    {
                        // null or empty column is found. Note column index starts from 0.
                        Debug.LogWarningFormat("Null or empty column is found at {0}.The celltype of {0} is '{1}' type.\n", i, title.GetCell(i).CellType);
                    }
                    else
                    {
                        // column header is not an empty string, we check its validation later.
                        result.Add(value);
                    }
                }

                return result.ToArray();
            }

            error = string.Format(@"Empty row at {0}", start);
            return null;
        }

        /// <summary>
        /// Convert type of cell value to its predefined type which is specified in the sheet's ScriptMachine setting file.
        /// </summary>
        protected object ConvertFrom(ICell cell, Type t)
        {
            object value = null;

            if (t == typeof(uint) || t == typeof(float) || t == typeof(double) || t == typeof(short) || t == typeof(int) || t == typeof(long))
            {
                if (cell.CellType == NPOI.SS.UserModel.CellType.Numeric)
                {
                    value = cell.NumericCellValue;
                }
                else if (cell.CellType == NPOI.SS.UserModel.CellType.String)
                {
                    //Get correct numeric value even the cell is string type but defined with a numeric type in a data class.
                    if (t == typeof(float))
                        value = Convert.ToSingle(cell.StringCellValue);
                    if (t == typeof(double))
                        value = Convert.ToDouble(cell.StringCellValue);
                    if (t == typeof(short))
                        value = Convert.ToInt16(cell.StringCellValue);
                    if (t == typeof(int))
                        value = Convert.ToInt32(cell.StringCellValue);
                    if (t == typeof(long))
                        value = Convert.ToInt64(cell.StringCellValue);
                    if(t == typeof(uint))
                    {
                        uint tmpValue = 0;
                        if (cell.StringCellValue == "-" || string.IsNullOrEmpty(cell.StringCellValue))
                            tmpValue = 0;
                        else if (!uint.TryParse(cell.StringCellValue.ToString(), out tmpValue))
                        {
                            byte[] idIntBytes = System.Text.Encoding.UTF8.GetBytes(cell.StringCellValue);
                            if (idIntBytes.Length == 4)
                            {
                                tmpValue = 0| ((uint)idIntBytes[0] << 24)
                                            | ((uint)idIntBytes[1] << 16)
                                            | ((uint)idIntBytes[2] << 8)
                                            | ((uint)idIntBytes[3] << 0);
                            }
                            else if (idIntBytes.Length > 4)
                            {
                                Debug.LogErrorFormat("ID:[{0}] is too long,length shoudn't be larger than 4",cell.StringCellValue);
                            }
                        }
                        else
                        {
                            Debug.LogErrorFormat("ID:[{0}] is not the 4-char format,convert to Numeric", cell.StringCellValue);
                        }
                        value = tmpValue;
                    }
                }
                else if (cell.CellType == NPOI.SS.UserModel.CellType.Formula)
                {
                    // Get value even if cell is a formula
                    if (t == typeof(float))
                        value = Convert.ToSingle(cell.NumericCellValue);
                    if (t == typeof(double))
                        value = Convert.ToDouble(cell.NumericCellValue);
                    if (t == typeof(short))
                        value = Convert.ToInt16(cell.NumericCellValue);
                    if (t == typeof(int))
                        value = Convert.ToInt32(cell.NumericCellValue);
                    if (t == typeof(long))
                        value = Convert.ToInt64(cell.NumericCellValue);
                }
            }
            else if (t == typeof(string) || t.IsArray)
            {
                // HACK: handles the case that a cell contains numeric value
                //       but a member field in a data class is defined as string type.
                //       e.g. string s = "123"
                if (cell.CellType == NPOI.SS.UserModel.CellType.Numeric)
                    value = cell.NumericCellValue;
                else
                    value = cell.StringCellValue;
            }
            else if (t == typeof(bool))
                value = cell.BooleanCellValue;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                var nc = new NullableConverter(t);
                return nc.ConvertFrom(value);
            }

            if (t.IsEnum)
            {
                // for enum type, first get value by string then convert it to enum.
                value = cell.StringCellValue;
                return Enum.Parse(t, value.ToString(), true);
            }
            else if (t.IsArray)
            {
                if (t.GetElementType() == typeof(float))
                    return ConvertExt.ToSingleArray((string)value);

                if (t.GetElementType() == typeof(double))
                    return ConvertExt.ToDoubleArray((string)value);

                if (t.GetElementType() == typeof(short))
                    return ConvertExt.ToInt16Array((string)value);

                if (t.GetElementType() == typeof(int))
                    return ConvertExt.ToInt32Array((string)value);

                if (t.GetElementType() == typeof(long))
                    return ConvertExt.ToInt64Array((string)value);

                if (t.GetElementType() == typeof(string))
                    return ConvertExt.ToStringArray((string)value);
            }

            // for all other types, convert its corresponding type.
            return Convert.ChangeType(value, t);
        }

        private Dictionary<string, int> bitwise_dic = new Dictionary<string, int>();
        private uint ConverBitwise(ICell cell,string comment)
        {
            uint result = 0;
            bitwise_dic.Clear();
            var pattern = @"\[([\s\S]*?)::([0-9]+)\]"; //[bitname::bitvalue]
            var matches = Regex.Matches(comment, pattern);
            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = int.Parse(match.Groups[2].Value);
                if (!bitwise_dic.ContainsKey(key))
                    bitwise_dic.Add(key, value);
            }
            var bitnames = cell.StringCellValue.Split('|');
            for(int i = 0; i < bitnames.Length; i++)
            {
                var bitname = bitnames[i];
                if (bitwise_dic.ContainsKey(bitname))
                    result |= (uint)1 << bitwise_dic[bitname];
                else
                    Debug.LogErrorFormat("Unknown bitname:{0}", bitname);
            }
            return result;
        }

        public enum TestType
        {

        }


        StringBuilder enumbuilder = new StringBuilder();
        private string DeserializeEnum(string EnumName, string Comment)
        {
            if (string.IsNullOrEmpty(EnumName))
                return string.Empty;
            enumbuilder.Clear();
            enumbuilder.AppendLine("public enum " + EnumName + "{");
            var pattern = @"\[([\s\S]*?)::([0-9]+)\]"; //[enum_name::enum_value]
            var matches = Regex.Matches(Comment, pattern);
            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var value = int.Parse(match.Groups[2].Value);
                enumbuilder.AppendFormat("\t{0} = {1},\n", name, value);
            }
            enumbuilder.AppendLine("}");
            return enumbuilder.ToString();
        }

        public string GetEnumDefine()
        {
            StringBuilder sb = new StringBuilder();
            if (sheet != null)
            {
                var row = sheet.GetRow(0);
                if(row != null)
                {
                    foreach(var cell in row.Cells)
                    {
                        if(cell.CellComment != null)
                        {
                            var comment = cell.CellComment.String.ToString();
                            if (comment.Contains("Enum") || comment.Contains("枚举"))
                                sb.AppendLine(DeserializeEnum(cell.StringCellValue, comment));
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}

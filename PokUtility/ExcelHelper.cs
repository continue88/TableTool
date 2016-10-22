using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExcelInterop = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.IO;

namespace PokUtility
{
    public static class ExcelHelper
    {
        public const int LetterCount = 26;

        public static string GetColumeLetter(int columnsCount)
        {
            string ret = "";
            while (columnsCount > LetterCount) 
            {
                var num = columnsCount / LetterCount;

                ret += (char)('A' + num - 1);
                columnsCount -= num * LetterCount;
            }
            return ret + (char)('A' + columnsCount - 1);
        }

        static object[,] ReadExcelEx(string fileName)
        {
            var application = new ExcelInterop.ApplicationClass();
            var missing = System.Reflection.Missing.Value;
            var workbook = application.Application.Workbooks.Open(fileName, missing, true, missing, missing, missing,
                missing, missing, missing, true, missing, missing, missing, missing, missing);
            var worksheet = (ExcelInterop.Worksheet)workbook.Worksheets.get_Item(1);

            var rowsCount = worksheet.UsedRange.Cells.Rows.Count;
            var columnsCount = worksheet.UsedRange.Cells.Columns.Count;
            var lastCell = GetColumeLetter(columnsCount) + rowsCount;
            var range = worksheet.Cells.get_Range("A1", lastCell);
            var ret = range.Value2 as object[,];

            workbook.Close(false, missing, missing);
            application.Quit();

            Marshal.ReleaseComObject(worksheet);
            Marshal.ReleaseComObject(workbook);
            Marshal.ReleaseComObject(application);

            return ret;
        }

        public static object[,] ReadExcel(string fileName)
        {
            try
            {
                var excelFile = new FileInfo(fileName);
                using (var package = new OfficeOpenXml.ExcelPackage(excelFile))
                {
                    var workSheet = package.Workbook.Worksheets[1];
                    var cells = workSheet.Cells;
                    var ret = Array.CreateInstance(typeof(object), new int[] { workSheet.Dimension.Rows, workSheet.Dimension.Columns }, new int[] { 1, 1 }) as object[,];
                    for (var i = 1; i <= workSheet.Dimension.Rows; i++)
                    {
                        for (var j = 1; j <= workSheet.Dimension.Columns; j++)
                            ret[i, j] = cells[i, j].Value;
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ReadExcelEx(fileName);
            }
        }

        public static void WriteExcelEx(string fileName, object[,] data)
        {
            var missing = System.Reflection.Missing.Value;
            var application = new ExcelInterop.ApplicationClass();
            try
            {
                var workbook = application.Workbooks.Add(missing);
                var worksheet = workbook.ActiveSheet as ExcelInterop.Worksheet;

                var rowsCount = data.GetLength(0);
                var columnsCount = data.GetLength(1);
                var lastCell = GetColumeLetter(columnsCount) + rowsCount;
                var range = worksheet.Cells.get_Range("A1", lastCell);
                range.NumberFormat = "@";
                range.Value2 = data;

                var format = fileName.EndsWith("xlsx") ?
                    ExcelInterop.XlFileFormat.xlOpenXMLWorkbook :
                    ExcelInterop.XlFileFormat.xlWorkbookNormal;

                workbook.SaveAs(fileName, format, missing, missing, missing, missing,
                    ExcelInterop.XlSaveAsAccessMode.xlExclusive, missing, missing, missing, missing, missing);
                workbook.Close(true, missing, missing);
                application.Quit();

                Marshal.ReleaseComObject(workbook);
                Marshal.ReleaseComObject(worksheet);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Marshal.ReleaseComObject(application);
            }
        }

        public static void WriteExcel(string fileName, object[,] data)
        {
            try
            {
                var excelFile = new FileInfo(fileName);
                using (var package = new OfficeOpenXml.ExcelPackage(excelFile))
                {
                    var workSheet = package.Workbook.Worksheets.Add("Sheet1");
                    var cells = workSheet.Cells;
                    var rows = data.GetLength(0);
                    var colums = data.GetLength(1);
                    for (var i = 1; i <= rows; i++)
                    {
                        for (var j = 1; j <= colums; j++)
                            cells[i, j].Value = data[i-1, j-1];
                    }
                    package.Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                WriteExcelEx(fileName, data);
            }
        }
    }
}

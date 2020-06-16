using System.Collections.Generic;
using System.Data;
using ClosedXML.Excel;

namespace AzDevOpsWiReader.Shared
{
    public class ExcelExport
    {
        public static XLWorkbook ConvertToExcel(Dictionary<string, DataTable> dts)
        {
            XLWorkbook wb = new XLWorkbook();
            foreach (var key in dts.Keys)
            {
                wb.AddWorksheet(dts[key], key);
            }
            return wb;
        }
    }
}

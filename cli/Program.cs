using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AzDevOpsWiReader.Shared;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;

namespace AzDevOpsWiReader.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json", optional: false, reloadOnChange: true);
            var config = builder.Build();
            var c = config.Get<Config>();

            var table = AzDevOpsReader.ReadWIs(c);

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Workitems");

            var insertedTable = ws.Cell(1, 1).InsertTable(table.AsEnumerable());

            var hyperlinkRegex = @"=HYPERLINK\((.*);(.*)\)";
            foreach (var row in insertedTable.Rows())
            {
                foreach (var col in insertedTable.Columns())
                {
                    var val = row.Cell(col.ColumnNumber()).GetValue<string>();
                    var matches = Regex.Matches(val, hyperlinkRegex);
                    if (matches.Count > 0)
                    {
                        row.Cell(col.ColumnNumber()).Value = matches[0].Groups[2].Value;
                        row.Cell(col.ColumnNumber()).Hyperlink = new XLHyperlink(matches[0].Groups[1].Value);
                    }
                }
            }
            ws.Columns().AdjustToContents();
            wb.SaveAs("WorkItems.xlsx");
        }
    }
}

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

            var azDevOpsResults = AzDevOpsReader.ReadWIs(c);

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Workitems");
            DataTable table = new DataTable();
            table.Columns.Add("ID");
            foreach (var field in azDevOpsResults.FieldList)
            {
                table.Columns.Add(field, typeof(string));
            }

            foreach (var wiDict in azDevOpsResults.WIs)
            {
                foreach (var wiKVP in wiDict)
                {
                    var row = table.NewRow();
                    row["ID"] = wiKVP.Key;
                    if (wiKVP.Value == null)
                    {
                        Console.WriteLine($"Details for Workitem {wiKVP.Key} are missing");
                    }
                    else
                    {
                        foreach (var field in azDevOpsResults.FieldList)
                        {
                            row[field] = wiKVP.Value.ContainsKey(field) ? wiKVP.Value[field] : "";
                            if (field == "System.Title")
                            {
                                if (wiKVP.Value.ContainsKey("URL"))
                                    row[field] = wiKVP.Value.ContainsKey(field) ? $"=HYPERLINK({wiKVP.Value["URL"]};{wiKVP.Value[field]})" : "";
                                else
                                    row[field] = wiKVP.Value.ContainsKey(field) ? $"{wiKVP.Value[field]}" : "";
                            }
                            else if (field == "ParentTitle")
                            {
                                if (wiKVP.Value.ContainsKey("ParentURL"))
                                    row[field] = wiKVP.Value.ContainsKey(field) ? $"=HYPERLINK({wiKVP.Value["ParentURL"]};{wiKVP.Value[field]})" : "";
                                else
                                    row[field] = wiKVP.Value.ContainsKey(field) ? $"{wiKVP.Value[field]}" : "";
                            }
                        }
                    }
                    table.Rows.Add(row);
                }
            }

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

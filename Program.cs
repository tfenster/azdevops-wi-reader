using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;

namespace AzDevOpsWiReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("orgs.json");
            var config = builder.Build();
            var c = config.Get<OrgsWithPATConfig>();
            var fieldList = new List<string>(c.Fields);
            if (!fieldList.Contains("System.Title"))
            {
                fieldList.Add("System.Title");
            }

            var tasks = new List<Task<ConcurrentDictionary<long, Dictionary<string, string>>>>();
            foreach (var orgWithPAT in c.OrgsWithPATs)
            {
                foreach (var org in orgWithPAT.Orgs)
                {
                    var or = new OrgReader(org, orgWithPAT.Pat, c.Query, fieldList, c.LinkType);
                    tasks.Add(or.ReadWIs());
                }
            }

            ConcurrentDictionary<long, Dictionary<string, string>>[] results = Task.WhenAll(tasks).Result;


            if (fieldList.Contains("System.AssignedTo"))
            {
                fieldList.Remove("System.AssignedTo");
                fieldList.Add("System.AssignedTo.DisplayName");
                fieldList.Add("System.AssignedTo.UniqueName");
            }
            fieldList.Add("ParentTitle");

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Workitems");
            DataTable table = new DataTable();
            table.Columns.Add("ID");
            foreach (var field in fieldList)
            {
                table.Columns.Add(field, typeof(string));
            }

            foreach (var wiDict in results)
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
                        foreach (var field in fieldList)
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

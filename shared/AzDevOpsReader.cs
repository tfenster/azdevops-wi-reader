using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace AzDevOpsWiReader.Shared
{
    public class AzDevOpsReader
    {
        public static DataTable ReadWIs(Config c)
        {
            var fieldList = new List<FieldWithLabel>(c.Fields);
            if (!fieldList.Any(f => f.Id == "System.Title"))
            {
                fieldList.Add(new FieldWithLabel() { Id = "System.Title", Label = "Title" });
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

            if (fieldList.Any(f => f.Id == "System.AssignedTo"))
            {
                fieldList.RemoveAll(f => f.Id == "System.AssignedTo");
                fieldList.Add(new FieldWithLabel() { Id = "System.AssignedTo.DisplayName", Label = "Name" });
                fieldList.Add(new FieldWithLabel() { Id = "System.AssignedTo.UniqueName", Label = "eMail" });
            }
            fieldList.Add(new FieldWithLabel() { Id = "ParentTitle", Label = "Parent" });

            var table = new DataTable();
            table.Columns.Add("ID", System.Type.GetType("System.Int32"));
            table.Columns.Add("URL", typeof(string));
            table.Columns.Add("ParentURL", typeof(string));
            foreach (var field in fieldList)
            {
                table.Columns.Add(field.Label, typeof(string));
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
                            row[field.Label] = wiKVP.Value.ContainsKey(field.Id) ? wiKVP.Value[field.Id] : "";
                            if (field.Id == "System.Title" && wiKVP.Value.ContainsKey("URL"))
                            {
                                row["URL"] = wiKVP.Value["URL"];
                            }
                            else if (field.Id == "ParentTitle" && wiKVP.Value.ContainsKey("ParentURL"))
                            {
                                row["ParentURL"] = wiKVP.Value["ParentURL"];
                            }
                        }
                    }
                    table.Rows.Add(row);
                }
            }

            return table;
        }
    }
}
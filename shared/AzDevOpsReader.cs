using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;

namespace AzDevOpsWiReader.Shared
{
    public class AzDevOpsReader
    {
        public static List<ExpandoObject> ReadWIs(Config c)
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
            foreach (var field in fieldList)
            {
                table.Columns.Add(field.Label, typeof(string));
            }

            var expandos = new List<ExpandoObject>();
            var firstExpando = new ExpandoObject();
            AddProperty(firstExpando, "ID", "ID");
            foreach (var field in fieldList)
            {
                AddProperty(firstExpando, field.Id, field.Label);
            }
            expandos.Add(firstExpando);

            foreach (var wiDict in results)
            {
                foreach (var wiKVP in wiDict)
                {
                    dynamic expando = new ExpandoObject();
                    AddProperty(expando, "ID", wiKVP.Key);
                    if (wiKVP.Value == null)
                    {
                        Console.WriteLine($"Details for Workitem {wiKVP.Key} are missing");
                    }
                    else
                    {
                        foreach (var field in fieldList)
                        {
                            AddProperty(expando, field.Id, wiKVP.Value.ContainsKey(field.Id) ? wiKVP.Value[field.Id] : "");
                            if (field.Id == "System.Title")
                            {
                                if (wiKVP.Value.ContainsKey("URL"))
                                    AddProperty(expando, "System.Title_Extended", wiKVP.Value.ContainsKey(field.Id) ? $"=HYPERLINK({wiKVP.Value["URL"]};{wiKVP.Value[field.Id]})" : "");
                                else
                                    AddProperty(expando, "System.Title_Extended", wiKVP.Value.ContainsKey(field.Id) ? wiKVP.Value[field.Id] : "");
                            }
                            else if (field.Id == "ParentTitle")
                            {
                                if (wiKVP.Value.ContainsKey("ParentURL"))
                                    AddProperty(expando, "ParentTitle_Extended", wiKVP.Value.ContainsKey(field.Id) ? $"=HYPERLINK({wiKVP.Value["ParentURL"]};{wiKVP.Value[field.Id]})" : "");
                                else
                                    AddProperty(expando, "ParentTitle_Extended", wiKVP.Value.ContainsKey(field.Id) ? wiKVP.Value[field.Id] : "");
                            }
                        }
                    }
                    expandos.Add(expando);
                }
            }

            return expandos;
        }

        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            propertyName = propertyName.Replace(".", "_");
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace AzDevOpsWiReader.Shared
{
    public class AzDevOpsReader
    {
        private const string Stakeholder = "Stakeholder";
        private const string Basic = "Basic";
        private const string BasicAndTestPlan = "Basic + Test Plans";
        private const string VisualStudioStart = "Visual Studio";
        private const double AzureExchangeEuro = 0.843; // https://azureprice.net/Exchange

        public static async Task<DataTable> ReadWIs(Config c)
        {
            var fieldList = new List<FieldWithLabel>(c.Fields);
            if (!fieldList.Any(f => f.Id == "System.Title"))
            {
                fieldList.Add(new FieldWithLabel() { Id = "System.Title", Label = "Title" });
            }

            var tasks = new List<Task<ConcurrentDictionary<long, Dictionary<string, string>>>>();
            var tasksEntities = new List<Task<KeyValuePair<string, string>>>();
            foreach (var orgWithPAT in c.OrgsWithPATs)
            {
                foreach (var org in orgWithPAT.Orgs)
                {
                    var or = new OrgReader(org, orgWithPAT.Pat, c.Query, fieldList, c.LinkType);
                    tasks.Add(or.ReadWIs());
                    var ur = new UserReader(org, orgWithPAT.Pat);
                    tasksEntities.Add(ur.ReadEntity());
                }
            }

            ConcurrentDictionary<long, Dictionary<string, string>>[] results = await Task.WhenAll(tasks);
            KeyValuePair<string, string>[] resultsEntitiesKVP = await Task.WhenAll(tasksEntities);
            var resultsEntities = resultsEntitiesKVP.ToDictionary(x => x.Key, x => x.Value);

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
            table.Columns.Add("Entity", typeof(string));
            table.Columns.Add("Organization", typeof(string));
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
                    row["Organization"] = wiKVP.Value["Organization"];
                    row["Entity"] = resultsEntities[wiKVP.Value["Organization"]];
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

        public static async Task<Dictionary<string, DataTable>> ReadUsers(Config c)
        {
            var tasksUsers = new List<Task<ConcurrentDictionary<string, Dictionary<string, string>>>>();
            var tasksLicenseSummaries = new List<Task<LicenseSummary>>();
            var tasksEntities = new List<Task<KeyValuePair<string, string>>>();
            var internalStakeholdersCount = new Dictionary<string, int>();
            var internalBasicsCount = new Dictionary<string, int>();
            var internalTestsCount = new Dictionary<string, int>();
            var internalVSCount = new Dictionary<string, int>();
            foreach (var orgWithPAT in c.OrgsWithPATs)
            {
                foreach (var org in orgWithPAT.Orgs)
                {
                    var ur = new UserReader(org, orgWithPAT.Pat);
                    tasksUsers.Add(ur.ReadUsers());
                    tasksLicenseSummaries.Add(ur.ReadLicenseSummary());
                    tasksEntities.Add(ur.ReadEntity());
                    internalStakeholdersCount[org] = 0;
                    internalBasicsCount[org] = 0;
                    internalTestsCount[org] = 0;
                    internalVSCount[org] = 0;
                }
            }

            ConcurrentDictionary<string, Dictionary<string, string>>[] resultsUsers = await Task.WhenAll(tasksUsers);
            LicenseSummary[] resultsLicenseSummaries = await Task.WhenAll(tasksLicenseSummaries);
            KeyValuePair<string, string>[] resultsEntitiesKVP = await Task.WhenAll(tasksEntities);
            var resultsEntities = resultsEntitiesKVP.ToDictionary(x => x.Key, x => x.Value);

            var tableUsers = new DataTable();
            var fieldsUsers = new string[] {
                "Entity", "Organization", "Display Name", "Mail Address", "Subject Kind", "License Type", "License Status", "Last Accessed"
            };
            foreach (var field in fieldsUsers)
            {
                tableUsers.Columns.Add(field, typeof(string));
            }

            foreach (var userDict in resultsUsers)
            {
                foreach (var userKVP in userDict)
                {
                    var row = tableUsers.NewRow();
                    foreach (var field in fieldsUsers.Where(f => f.CompareTo("Entity") != 0))
                    {
                        row[field] = userKVP.Value[field];
                    }
                    row["Entity"] = resultsEntities[userKVP.Value["Organization"]];
                    if (!string.IsNullOrEmpty(userKVP.Value["Mail Address"]) && !string.IsNullOrEmpty(userKVP.Value["License Type"]) && userKVP.Value["Mail Address"].EndsWith(c.InternalDomain))
                    {
                        if (userKVP.Value["License Type"] == Stakeholder)
                            internalStakeholdersCount[userKVP.Value["Organization"]] += 1;
                        else if (userKVP.Value["License Type"] == Basic)
                            internalBasicsCount[userKVP.Value["Organization"]] += 1;
                        else if (userKVP.Value["License Type"] == BasicAndTestPlan)
                            internalTestsCount[userKVP.Value["Organization"]] += 1;
                        else if (userKVP.Value["License Type"].StartsWith(VisualStudioStart))
                            internalVSCount[userKVP.Value["Organization"]] += 1;
                    }
                    tableUsers.Rows.Add(row);
                }
            }
            var tables = new Dictionary<string, DataTable>();
            tables.Add("Users", tableUsers);

            var tableLicenseSummaries = new DataTable();
            var fieldsLicenseSummaries = new string[] {
                "Entity", "Organization", "Stakeholders Total", "Stakeholders External", "Basic Total", "Basic External", "Basic Included", "Basic Price", "Test Total", "Test External", "Test Price", "Visual Studio Total", "Visual Studio External", "Price Total"
            };
            //var currency = await CurrencyReader.ReadCurrency(); --> see const AzureExchangeEuro
            var pricing = await AzureReader.ReadPricing();
            var basicPrice = Math.Round(AzureExchangeEuro * pricing.GraduatedOffers.UserPlanBasic.Global.Prices.Where(p => p.PricePrice.Value != 0D).First().PricePrice.Value * 100) / 100;
            var testPrice = Math.Round(AzureExchangeEuro * pricing.Offers.TestManager.Prices.Global.Value * 100) / 100;

            foreach (var field in fieldsLicenseSummaries)
            {
                tableLicenseSummaries.Columns.Add(field, typeof(string));
            }
            foreach (var ls in resultsLicenseSummaries)
            {
                var row = tableLicenseSummaries.NewRow();
                foreach (var field in fieldsLicenseSummaries)
                {
                    row[field] = "";
                }
                row["Organization"] = ls.Organization;
                row["Entity"] = resultsEntities[ls.Organization];

                var stakeholderLicense = ls.Licenses.Where(l => l.LicenseName == Stakeholder).FirstOrDefault();
                if (stakeholderLicense != null)
                {
                    row["Stakeholders Total"] = stakeholderLicense.Assigned;
                    row["Stakeholders External"] = ((int)stakeholderLicense.Assigned - internalStakeholdersCount[ls.Organization]);
                }

                var totalPrice = 0D;

                var basicLicense = ls.Licenses.Where(l => l.LicenseName == Basic).FirstOrDefault();
                if (basicLicense != null)
                {
                    row["Basic Total"] = basicLicense.Assigned;
                    row["Basic External"] = ((int)basicLicense.Assigned - internalBasicsCount[ls.Organization]);
                    row["Basic Included"] = basicLicense.IncludedQuantity;
                    var externalBasicsNotIncluded = (int)basicLicense.Assigned - internalBasicsCount[ls.Organization] - (int)basicLicense.IncludedQuantity;
                    var cummBasicPrice = externalBasicsNotIncluded > 0 ? Math.Round(externalBasicsNotIncluded * basicPrice * 100) / 100 : 0D;
                    totalPrice += cummBasicPrice;
                    row["Basic Price"] = cummBasicPrice.ToString();
                }

                var testLicense = ls.Licenses.Where(l => l.LicenseName == BasicAndTestPlan).FirstOrDefault();
                if (testLicense != null)
                {
                    row["Test Total"] = testLicense.Assigned;
                    row["Test External"] = ((int)testLicense.Assigned - internalTestsCount[ls.Organization]);
                    var cummTestPrice = Math.Round(((int)testLicense.Assigned - internalTestsCount[ls.Organization]) * testPrice * 100) / 100;
                    totalPrice += cummTestPrice;
                    row["Test Price"] = cummTestPrice.ToString();
                }

                row["Price Total"] = totalPrice.ToString();

                var vsLicenses = ls.Licenses.Where(l => l.LicenseName.StartsWith(VisualStudioStart));
                var vsTotal = 0L;
                foreach (var vsLicense in vsLicenses)
                {
                    vsTotal += vsLicense.Assigned;
                }
                row["Visual Studio Total"] = vsTotal;
                row["Visual Studio External"] = ((int)vsTotal - internalVSCount[ls.Organization]);

                tableLicenseSummaries.Rows.Add(row);
            }

            tables.Add("License Info", tableLicenseSummaries);
            return tables;
        }

        public static async Task<DataTable> ReadHistory(Config c)
        {
            var tasks = new List<Task<ConcurrentDictionary<Guid, Dictionary<string, string>>>>();
            var fieldList = new List<FieldWithLabel>();
            fieldList.Add(new FieldWithLabel() { Id = "System.TeamProject", Label = "Project" });
            fieldList.Add(new FieldWithLabel() { Id = "System.Title", Label = "Title" });
            fieldList.Add(new FieldWithLabel() { Id = "ParentTitle", Label = "Parent" });
            fieldList.Add(new FieldWithLabel() { Id = "TimeChangedAt", Label = "Time changed At" });
            fieldList.Add(new FieldWithLabel() { Id = "TimeChange", Label = "Time change" });
            var table = new DataTable();
            //table.Columns.Add("ID", typeof(Guid));
            table.Columns.Add("URL", typeof(string));
            table.Columns.Add("ParentURL", typeof(string));
            table.Columns.Add("Organization", typeof(string));
            //table.Columns.Add("Time changed At", typeof(string));
            //table.Columns.Add("Time change", typeof(float));
            foreach (var field in fieldList)
            {
                table.Columns.Add(field.Label, typeof(string));
            }

            foreach (var orgWithPAT in c.OrgsWithPATs)
            {
                foreach (var org in orgWithPAT.Orgs)
                {
                    var or = new OrgReader(org, orgWithPAT.Pat, c.Query, fieldList, c.LinkType);
                    tasks.Add(or.ReadWIsWithTimeChange());
                }
            }

            ConcurrentDictionary<Guid, Dictionary<string, string>>[] results = await Task.WhenAll(tasks);
            foreach (var wiDict in results)
            {
                foreach (var wiKVP in wiDict)
                {
                    var row = table.NewRow();
                    //row["ID"] = wiKVP.Key;
                    row["Organization"] = wiKVP.Value["Organization"];
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

            table.DefaultView.Sort = "Time changed At desc";
            return table.DefaultView.ToTable();
        }
    }
}
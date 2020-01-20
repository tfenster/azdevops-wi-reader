namespace AzDevOpsWiReader
{
    public class OrgsWithPATConfig
    {
        public OrgsWithPAT[] OrgsWithPATs { get; set; }
        public string Query { get; set; }
        public string LinkType { get; set; }
        public string[] Fields { get; set; }
    }

    public class OrgsWithPAT
    {
        public string Pat { get; set; }
        public string[] Orgs { get; set; }
    }
}
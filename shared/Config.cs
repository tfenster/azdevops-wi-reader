namespace AzDevOpsWiReader.Shared
{
    public class Config
    {
        public OrgsWithPAT[] OrgsWithPATs { get; set; }
        public string Query { get; set; }
        public string LinkType { get; set; }
        public FieldWithLabel[] Fields { get; set; }
    }

    public class OrgsWithPAT
    {
        public string Pat { get; set; }
        public string[] Orgs { get; set; }
    }

    public class FieldWithLabel
    {
        public string Id { get; set; }
        public string Label { get; set; }
    }
}
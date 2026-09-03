namespace Brandmauer;

public struct NameCom
{
    public struct Domain
    {
        public string DomainName { get; set; }
    }
    public List<Domain> Domains { get; set; }

    public struct Record
    {
        public int Id { get; set; }
        public string DomainName { get; set; }
        public string Host { get; set; }
        public string Fqdn { get; set; }
        public string Type { get; set; }
        public string Answer { get; set; }
        public int Ttl { get; set; }
    }
    public List<Record> Records { get; set; }
}

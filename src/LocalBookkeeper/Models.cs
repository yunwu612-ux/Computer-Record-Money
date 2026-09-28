namespace LocalBookkeeper;

public class LedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Type { get; set; } = "支出";
    public string Category { get; set; } = "其他";
    public string Note { get; set; } = "";
}

public class LedgerData
{
    public int DataVersion { get; set; } = 1;
    public List<LedgerEntry> Entries { get; set; } = new();
}

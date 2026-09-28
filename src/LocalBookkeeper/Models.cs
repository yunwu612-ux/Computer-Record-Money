namespace LocalBookkeeper;

public enum TransactionType
{
    Expense,
    Income,
    Payable,
    Receivable
}

public class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "现金";
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class TransactionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = "";
    public string Note { get; set; } = "";
    public string AccountId { get; set; } = "";
    public bool IsSettled { get; set; }
}

public class LedgerData
{
    public int DataVersion { get; set; } = 2;
    public List<Account> Accounts { get; set; } = new();
    public List<TransactionItem> Transactions { get; set; } = new();
}

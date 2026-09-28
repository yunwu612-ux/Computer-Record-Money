using System;
using System.Collections.Generic;
namespace LocalBookkeeper;
public enum TransactionType { Expense, Income, Payable, Receivable }
public sealed class Account {
 public Guid Id { get; set; } = Guid.NewGuid();
 public string Name { get; set; } = "";
 public decimal Balance { get; set; }
 public DateTime CreatedAt { get; set; } = DateTime.Now;
}
public sealed class TransactionItem {
 public Guid Id { get; set; } = Guid.NewGuid();
 public DateTime Date { get; set; } = DateTime.Today;
 public decimal Amount { get; set; }
 public TransactionType Type { get; set; } = TransactionType.Expense;
 public string Category { get; set; } = "其他";
 public string Note { get; set; } = "";
 public Guid? AccountId { get; set; }
 public bool IsSettled { get; set; }
}
public sealed class LedgerData {
 public int DataVersion { get; set; } = 2;
 public List<Account> Accounts { get; set; } = new();
 public List<TransactionItem> Transactions { get; set; } = new();
}

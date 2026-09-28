using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace LocalBookkeeper;

public partial class MainWindow : Window
{
    private readonly StorageService storage = new();
    private LedgerData data = new();
    private readonly ObservableCollection<RecordView> visibleRecords = new();

    public MainWindow()
    {
        InitializeComponent();
        data = storage.Load();
        TypeBox.SelectedIndex = 0;
        DateBox.SelectedDate = DateTime.Today;
        RefreshAccounts();
        Refresh();
    }

    private void RefreshAccounts()
    {
        var selected = AccountBox.SelectedValue as string;
        AccountBox.ItemsSource = data.Accounts;
        AccountBox.DisplayMemberPath = "Name";
        AccountBox.SelectedValuePath = "Id";
        if (selected != null && data.Accounts.Any(a => a.Id == selected))
            AccountBox.SelectedValue = selected;
        else
            AccountBox.SelectedIndex = 0;
    }

    private void Refresh()
    {
        var now = DateTime.Now;
        var month = data.Transactions.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month);
        var assets = data.Accounts.Sum(a => a.Balance);

        AssetText.Text = assets.ToString("N2");
        IncomeText.Text = month.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount).ToString("N2");
        ExpenseText.Text = month.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount).ToString("N2");
        PayableText.Text = data.Transactions.Where(t => t.Type == TransactionType.Payable && !t.IsSettled).Sum(t => t.Amount).ToString("N2");
        ReceivableText.Text = data.Transactions.Where(t => t.Type == TransactionType.Receivable && !t.IsSettled).Sum(t => t.Amount).ToString("N2");
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        visibleRecords.Clear();
        var q = SearchBox?.Text?.Trim() ?? "";
        foreach (var t in data.Transactions.OrderByDescending(x => x.Date))
        {
            var typeName = GetTypeName(t.Type);
            var accountName = data.Accounts.FirstOrDefault(a => a.Id == t.AccountId)?.Name ?? "—";
            if (!string.IsNullOrWhiteSpace(q) &&
                !typeName.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !t.Category.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !t.Note.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !accountName.Contains(q, StringComparison.OrdinalIgnoreCase))
                continue;

            visibleRecords.Add(new RecordView
            {
                Id = t.Id, Date = t.Date, Amount = t.Amount, TypeName = typeName,
                AccountName = accountName, Category = t.Category, Note = t.Note
            });
        }
        RecordsGrid.ItemsSource = visibleRecords;
    }

    private static string GetTypeName(TransactionType type) => type switch
    {
        TransactionType.Income => "收入",
        TransactionType.Expense => "支出",
        TransactionType.Payable => "我欠的 / 应付款",
        TransactionType.Receivable => "别人欠我 / 待收款",
        _ => "其他"
    };

    private static bool TryParseAmount(string input, out decimal amount)
    {
        input = (input ?? "").Trim()
            .Replace("，", ".").Replace("。", ".")
            .Replace(",", "").Replace("￥", "").Replace("¥", "").Replace("$", "");
        return decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
               && amount > 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseAmount(AmountBox.Text, out var amount))
        {
            MessageBox.Show("请输入大于 0 的有效金额。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            AmountBox.Focus();
            return;
        }

        if (AccountBox.SelectedValue is not string accountId)
        {
            MessageBox.Show("请先选择账户。");
            return;
        }

        var type = (TypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Income" => TransactionType.Income,
            "Payable" => TransactionType.Payable,
            "Receivable" => TransactionType.Receivable,
            _ => TransactionType.Expense
        };

        var account = data.Accounts.First(a => a.Id == accountId);
        if (type == TransactionType.Income) account.Balance += amount;
        if (type == TransactionType.Expense)
        {
            if (account.Balance < amount)
            {
                var result = MessageBox.Show("当前账户余额不足，仍要记录这笔支出吗？",
                    "余额提醒", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }
            account.Balance -= amount;
        }

        data.Transactions.Add(new TransactionItem
        {
            Date = DateBox.SelectedDate ?? DateTime.Today,
            Amount = amount,
            Type = type,
            Category = CategoryBox.Text.Trim(),
            Note = NoteBox.Text.Trim(),
            AccountId = accountId
        });

        storage.Save(data);
        AmountBox.Clear();
        CategoryBox.Clear();
        NoteBox.Clear();
        DateBox.SelectedDate = DateTime.Today;
        Refresh();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not RecordView row) return;
        var t = data.Transactions.FirstOrDefault(x => x.Id == row.Id);
        if (t == null) return;

        var result = MessageBox.Show("确定删除这条记录吗？", "删除确认",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var account = data.Accounts.FirstOrDefault(a => a.Id == t.AccountId);
        if (account != null)
        {
            if (t.Type == TransactionType.Income) account.Balance -= t.Amount;
            if (t.Type == TransactionType.Expense) account.Balance += t.Amount;
        }

        data.Transactions.Remove(t);
        storage.Save(data);
        Refresh();
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e) => RefreshGrid();

    private void Accounts_Click(object sender, RoutedEventArgs e)
    {
        var win = new AccountWindow(data, storage) { Owner = this };
        win.ShowDialog();
        data = storage.Load();
        RefreshAccounts();
        Refresh();
    }
}

public class RecordView
{
    public string Id { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string TypeName { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Note { get; set; } = "";
}

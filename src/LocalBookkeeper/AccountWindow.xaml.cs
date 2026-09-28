using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace LocalBookkeeper;

public partial class AccountWindow : Window
{
    private readonly LedgerData data;
    private readonly StorageService storage;
    private readonly ObservableCollection<Account> accounts = new();

    public AccountWindow(LedgerData data, StorageService storage)
    {
        InitializeComponent();
        this.data = data;
        this.storage = storage;
        Reload();
    }

    private void Reload()
    {
        accounts.Clear();
        foreach (var a in data.Accounts) accounts.Add(a);
        Grid.ItemsSource = accounts;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("请输入账户名称。");
            return;
        }

        var text = BalanceBox.Text.Trim().Replace("，", ".").Replace(",", "");
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var balance))
            balance = 0;

        data.Accounts.Add(new Account { Name = name, Balance = balance });
        storage.Save(data);
        NameBox.Clear();
        BalanceBox.Clear();
        Reload();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as System.Windows.Controls.Button)?.DataContext is not Account account) return;
        if (data.Accounts.Count <= 1)
        {
            MessageBox.Show("至少需要保留一个账户。");
            return;
        }

        if (data.Transactions.Any(t => t.AccountId == account.Id))
        {
            MessageBox.Show("该账户已有账目记录，暂时不能删除。");
            return;
        }

        if (MessageBox.Show($"确定删除“{account.Name}”吗？", "确认",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        data.Accounts.Remove(account);
        storage.Save(data);
        Reload();
    }
}

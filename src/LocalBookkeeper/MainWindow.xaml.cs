using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace LocalBookkeeper;

public partial class MainWindow : Window
{
    private readonly StorageService storage = new();
    private LedgerData data;
    private readonly ObservableCollection<LedgerEntry> visibleEntries = new();

    public MainWindow()
    {
        InitializeComponent();

        data = storage.Load();

        DateBox.SelectedDate = DateTime.Today;
        CategoryBox.SelectedIndex = 0;
        Refresh();
    }

    private void Refresh()
    {
        var query = SearchBox?.Text?.Trim() ?? "";

        var filtered = data.Entries
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Where(x =>
                string.IsNullOrWhiteSpace(query) ||
                x.Note.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        visibleEntries.Clear();
        foreach (var e in filtered)
            visibleEntries.Add(e);

        EntriesGrid.ItemsSource = visibleEntries;

        var now = DateTime.Today;
        var month = data.Entries.Where(x => x.Date.Year == now.Year && x.Date.Month == now.Month);

        var income = month.Where(x => x.Type == "收入").Sum(x => x.Amount);
        var expense = month.Where(x => x.Type == "支出").Sum(x => x.Amount);

        IncomeText.Text = $"¥ {income:N2}";
        ExpenseText.Text = $"¥ {expense:N2}";
        BalanceText.Text = $"¥ {income - expense:N2}";
        StatusText.Text = $"共 {data.Entries.Count} 笔 · 已自动保存";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountBox.Text.Trim(), out var amount) || amount <= 0)
        {
            MessageBox.Show("请输入正确的金额。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var type = (TypeBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "支出";
        var category = (CategoryBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "其他";

        data.Entries.Add(new LedgerEntry
        {
            Amount = amount,
            Type = type,
            Category = category,
            Date = DateBox.SelectedDate ?? DateTime.Today,
            Note = NoteBox.Text.Trim()
        });

        storage.Save(data);

        AmountBox.Clear();
        NoteBox.Clear();
        DateBox.SelectedDate = DateTime.Today;

        Refresh();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (EntriesGrid.SelectedItem is not LedgerEntry selected)
            return;

        var result = MessageBox.Show(
            $"确定删除这笔 {selected.Amount:N2} 的账单吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        data.Entries.RemoveAll(x => x.Id == selected.Id);
        storage.Save(data);
        Refresh();
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => Refresh();

    private void EntriesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (EntriesGrid.SelectedItem is not LedgerEntry selected)
            return;

        AmountBox.Text = selected.Amount.ToString("0.##");
        DateBox.SelectedDate = selected.Date;
        NoteBox.Text = selected.Note;

        foreach (System.Windows.Controls.ComboBoxItem item in TypeBox.Items)
            if (item.Content?.ToString() == selected.Type) TypeBox.SelectedItem = item;

        foreach (System.Windows.Controls.ComboBoxItem item in CategoryBox.Items)
            if (item.Content?.ToString() == selected.Category) CategoryBox.SelectedItem = item;
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        storage.Save(data);
        storage.CreateManualBackup();

        MessageBox.Show(
            "备份已保存到本地 AppData\\LocalBookkeeper\\backups 文件夹。",
            "备份成功",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择账本备份",
            Filter = "账本备份 (*.json)|*.json|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            if (!storage.RestoreFromFile(dialog.FileName))
            {
                MessageBox.Show("备份文件无法读取。", "恢复失败",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            data = storage.Load();
            Refresh();

            MessageBox.Show("账本恢复成功。", "恢复成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"恢复失败：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AmountBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(c => char.IsDigit(c) || c == '.');
    }
}

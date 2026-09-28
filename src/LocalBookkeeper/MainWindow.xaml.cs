using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LocalBookkeeper;
public partial class MainWindow : Window {
 private readonly StorageService storage=new();
 private LedgerData data=new();
 private List<TransactionRow> rows=new();
 public MainWindow(){InitializeComponent();TypeBox.SelectedIndex=0;CategoryBox.SelectedIndex=0;DateBox.SelectedDate=DateTime.Today;LoadData();}
 void LoadData(){data=storage.Load();if(data.Accounts.Count==0)data.Accounts.Add(new Account{Name="现金"});RefreshAccounts();RefreshView();}
 void RefreshAccounts(){var id=(AccountBox.SelectedItem as AccountChoice)?.Id;AccountBox.ItemsSource=data.Accounts.Select(a=>new AccountChoice(a.Id,$"{a.Name}（¥ {a.Balance:N2}）")).ToList();if(id.HasValue)AccountBox.SelectedItem=AccountBox.Items.Cast<AccountChoice>().FirstOrDefault(x=>x.Id==id);if(AccountBox.SelectedIndex<0&&AccountBox.Items.Count>0)AccountBox.SelectedIndex=0;}
 void RefreshView(){UpdateSummary();var q=SearchBox?.Text?.Trim()??"";rows=data.Transactions.OrderByDescending(t=>t.Date).ThenByDescending(t=>t.Id).Select(ToRow).Where(r=>string.IsNullOrWhiteSpace(q)||r.TypeName.Contains(q,StringComparison.OrdinalIgnoreCase)||r.Category.Contains(q,StringComparison.OrdinalIgnoreCase)||r.Note.Contains(q,StringComparison.OrdinalIgnoreCase)||r.AccountName.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();TransactionsGrid.ItemsSource=null;TransactionsGrid.ItemsSource=rows;}
 TransactionRow ToRow(TransactionItem t){var a=data.Accounts.FirstOrDefault(x=>x.Id==t.AccountId);return new TransactionRow{Id=t.Id,Date=t.Date,Amount=t.Amount,TypeName=TypeText(t.Type),Category=t.Category,Note=t.Note,AccountName=a?.Name??"未指定",IsSettled=t.IsSettled};}
 void UpdateSummary(){var m=DateTime.Today;var tx=data.Transactions.Where(t=>t.Date.Year==m.Year&&t.Date.Month==m.Month);TotalBalanceText.Text=$"¥ {data.Accounts.Sum(a=>a.Balance):N2}";IncomeText.Text=$"¥ {tx.Where(t=>t.Type==TransactionType.Income).Sum(t=>t.Amount):N2}";ExpenseText.Text=$"¥ {tx.Where(t=>t.Type==TransactionType.Expense).Sum(t=>t.Amount):N2}";PayableText.Text=$"¥ {data.Transactions.Where(t=>t.Type==TransactionType.Payable&&!t.IsSettled).Sum(t=>t.Amount):N2}";ReceivableText.Text=$"¥ {data.Transactions.Where(t=>t.Type==TransactionType.Receivable&&!t.IsSettled).Sum(t=>t.Amount):N2}";}
 void SaveTransaction_Click(object sender,RoutedEventArgs e){
  var raw=AmountBox.Text.Trim().Replace("，",",").Replace("。",".").Replace("￥","").Replace("¥","").Replace(" ","");
  if(!decimal.TryParse(raw,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount)||amount<=0){MessageBox.Show("请输入有效的正数金额，例如：25.50","金额有误",MessageBoxButton.OK,MessageBoxImage.Warning);AmountBox.Focus();return;}
  if(TypeBox.SelectedItem is not ComboBoxItem ti||!Enum.TryParse<TransactionType>(ti.Tag?.ToString(),out var type)){MessageBox.Show("请选择记录类型。");return;}
  var ac=AccountBox.SelectedItem as AccountChoice;var cat=(CategoryBox.SelectedItem as ComboBoxItem)?.Content?.ToString()??"其他";var date=DateBox.SelectedDate??DateTime.Today;
  var t=new TransactionItem{Amount=amount,Type=type,Category=cat,Date=date,Note=NoteBox.Text.Trim(),AccountId=ac?.Id};
  if(ac is not null&&(type==TransactionType.Income||type==TransactionType.Expense)){var a=data.Accounts.FirstOrDefault(x=>x.Id==ac.Id);if(a is not null)a.Balance+=type==TransactionType.Income?amount:-amount;}
  data.Transactions.Add(t);
  try{storage.Save(data);ClearForm();RefreshAccounts();RefreshView();StatusText.Text=$"已保存：{date:yyyy-MM-dd} · {TypeText(type)} · ¥ {amount:N2}";}catch(Exception ex){MessageBox.Show($"保存失败：{ex.Message}","保存失败",MessageBoxButton.OK,MessageBoxImage.Error);}
 }
 void ClearForm(){AmountBox.Clear();NoteBox.Clear();CategoryBox.SelectedIndex=0;DateBox.SelectedDate=DateTime.Today;TypeBox.SelectedIndex=0;AmountBox.Focus();}
 void ClearForm_Click(object sender,RoutedEventArgs e)=>ClearForm();
 void TypeBox_SelectionChanged(object sender,SelectionChangedEventArgs e){if(FormHint==null||TypeBox.SelectedItem is not ComboBoxItem i)return;FormHint.Text=i.Tag?.ToString()=="Payable"?"记录你需要支付给别人的金额，不会直接减少账户余额。":i.Tag?.ToString()=="Receivable"?"记录别人需要支付给你的金额，不会直接增加账户余额。":"收入会增加账户余额，支出会减少账户余额。";}
 void Accounts_Click(object sender,RoutedEventArgs e){var d=new AccountWindow(data.Accounts){Owner=this};if(d.ShowDialog()==true){storage.Save(data);RefreshAccounts();RefreshView();}}
 void Backup_Click(object sender,RoutedEventArgs e){try{storage.CreateManualBackup();MessageBox.Show("备份已创建。","备份成功");}catch(Exception ex){MessageBox.Show($"备份失败：{ex.Message}");}}
 void Restore_Click(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Title="选择账本备份",Filter="账本 JSON (*.json)|*.json|所有文件 (*.*)|*.*"};if(d.ShowDialog()!=true)return;if(MessageBox.Show("恢复会覆盖当前数据，并先自动备份当前账本。继续吗？","确认恢复",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;if(storage.RestoreFromFile(d.FileName)){LoadData();MessageBox.Show("恢复成功。","恢复完成");}else MessageBox.Show("无法读取这个备份文件。","恢复失败");}
 void DeleteTransaction_Click(object sender,RoutedEventArgs e){if(TransactionsGrid.SelectedItem is not TransactionRow r){MessageBox.Show("请先选择一条记录。");return;}if(MessageBox.Show("确定删除吗？账户余额会按原记录反向恢复。","确认删除",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;var t=data.Transactions.FirstOrDefault(x=>x.Id==r.Id);if(t==null)return;if(t.AccountId.HasValue&&(t.Type==TransactionType.Income||t.Type==TransactionType.Expense)){var a=data.Accounts.FirstOrDefault(x=>x.Id==t.AccountId.Value);if(a!=null)a.Balance+=t.Type==TransactionType.Income?-t.Amount:t.Amount;}data.Transactions.Remove(t);storage.Save(data);RefreshAccounts();RefreshView();}
 void SearchBox_TextChanged(object sender,TextChangedEventArgs e){if(TransactionsGrid!=null)RefreshView();}
 static string TypeText(TransactionType t)=>t switch{TransactionType.Income=>"收入",TransactionType.Expense=>"支出",TransactionType.Payable=>"我欠的 / 应付款",TransactionType.Receivable=>"别人欠我 / 待收款",_=>"其他"};
 public sealed record AccountChoice(Guid Id,string Display){public override string ToString()=>Display;}
 public sealed class TransactionRow{public Guid Id{get;set;}public DateTime Date{get;set;}public decimal Amount{get;set;}public string TypeName{get;set;}="";public string Category{get;set;}="";public string Note{get;set;}="";public string AccountName{get;set;}="";public bool IsSettled{get;set;}}
}

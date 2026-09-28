using System; using System.Collections.Generic; using System.Globalization; using System.Linq; using System.Windows;
namespace LocalBookkeeper;
public partial class AccountWindow : Window {
 private readonly List<Account> accounts;
 public AccountWindow(List<Account> accounts){InitializeComponent();this.accounts=accounts;Refresh();}
 private void Refresh(){AccountsGrid.ItemsSource=null;AccountsGrid.ItemsSource=accounts;TotalText.Text=$"账户总资产：¥ {accounts.Sum(a=>a.Balance):N2}";}
 private void Add_Click(object sender,RoutedEventArgs e){
  var name=NameBox.Text.Trim(); var raw=BalanceBox.Text.Trim().Replace("，",",").Replace("。",".").Replace("￥","").Replace("¥","").Replace(" ","");
  if(string.IsNullOrWhiteSpace(name)){MessageBox.Show("请输入账户名称。");return;}
  if(!decimal.TryParse(raw,NumberStyles.Number,CultureInfo.InvariantCulture,out var balance)||balance<0){MessageBox.Show("请输入有效余额，例如 1000.00。");return;}
  accounts.Add(new Account{Name=name,Balance=balance});NameBox.Clear();BalanceBox.Clear();Refresh();
 }
 private void Delete_Click(object sender,RoutedEventArgs e){
  if(AccountsGrid.SelectedItem is not Account a){MessageBox.Show("请先选择账户。");return;}
  if(accounts.Count<=1){MessageBox.Show("至少保留一个账户。");return;}
  if(MessageBox.Show($"确定删除账户“{a.Name}”吗？","确认删除",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes){accounts.Remove(a);Refresh();}
 }
 private void Done_Click(object sender,RoutedEventArgs e){DialogResult=true;Close();}
}

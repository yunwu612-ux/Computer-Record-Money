using System;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace LocalBookkeeper;
public sealed class StorageService {
 private readonly string dataFile;
 private readonly string backupDir;
 public StorageService() {
  var appDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"LocalBookkeeper");
  dataFile=Path.Combine(appDir,"data","ledger.json"); backupDir=Path.Combine(appDir,"backups");
  Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!); Directory.CreateDirectory(backupDir);
 }
 public LedgerData Load() {
  try {
   if(!File.Exists(dataFile)) return Default();
   var d=JsonSerializer.Deserialize<LedgerData>(File.ReadAllText(dataFile))??Default();
   d.Accounts??=new(); d.Transactions??=new(); d.DataVersion=Math.Max(d.DataVersion,2);
   if(d.Accounts.Count==0)d.Accounts.Add(new Account{Name="现金"});
   return d;
  } catch { return Default(); }
 }
 public void Save(LedgerData d) {
  Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!); Directory.CreateDirectory(backupDir);
  d.DataVersion=2; var tmp=dataFile+".tmp";
  File.WriteAllText(tmp,JsonSerializer.Serialize(d,new JsonSerializerOptions{WriteIndented=true}));
  if(File.Exists(dataFile)) try { File.Copy(dataFile,Path.Combine(backupDir,$"ledger-{DateTime.Now:yyyyMMdd-HHmmssfff}.json"),true); } catch {}
  File.Move(tmp,dataFile,true); Cleanup();
 }
 public void CreateManualBackup() {
  if(!File.Exists(dataFile))return;
  File.Copy(dataFile,Path.Combine(backupDir,$"manual-backup-{DateTime.Now:yyyyMMdd-HHmmssfff}.json"),true);
 }
 public bool RestoreFromFile(string source) {
  try {
   var d=JsonSerializer.Deserialize<LedgerData>(File.ReadAllText(source));
   if(d is null)return false; d.Accounts??=new(); d.Transactions??=new();
   if(d.Accounts.Count==0)d.Accounts.Add(new Account{Name="现金"});
   if(File.Exists(dataFile))try{File.Copy(dataFile,Path.Combine(backupDir,$"before-restore-{DateTime.Now:yyyyMMdd-HHmmssfff}.json"),true);}catch{}
   var tmp=dataFile+".restore.tmp"; File.WriteAllText(tmp,JsonSerializer.Serialize(d,new JsonSerializerOptions{WriteIndented=true})); File.Move(tmp,dataFile,true); return true;
  } catch{return false;}
 }
 private LedgerData Default(){var d=new LedgerData();d.Accounts.Add(new Account{Name="现金"});return d;}
 private void Cleanup(){try{foreach(var f in new DirectoryInfo(backupDir).GetFiles("ledger-*.json").OrderByDescending(x=>x.CreationTimeUtc).Skip(30))try{f.Delete();}catch{}}catch{}}
}

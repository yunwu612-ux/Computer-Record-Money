using System.IO;
using System.Text.Json;

namespace LocalBookkeeper;

public class StorageService
{
    private readonly string folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LocalBookkeeper");
    private string DataFile => Path.Combine(folder, "data", "ledger.json");
    private string BackupFolder => Path.Combine(folder, "backups");

    private readonly JsonSerializerOptions options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public LedgerData Load()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        Directory.CreateDirectory(BackupFolder);

        if (!File.Exists(DataFile))
        {
            var fresh = new LedgerData();
            fresh.Accounts.Add(new Account { Name = "现金", Balance = 0 });
            Save(fresh);
            return fresh;
        }

        try
        {
            var json = File.ReadAllText(DataFile);
            var data = JsonSerializer.Deserialize<LedgerData>(json, options) ?? new LedgerData();

            if (data.Accounts.Count == 0)
                data.Accounts.Add(new Account { Name = "现金", Balance = 0 });

            if (data.DataVersion < 2)
                data.DataVersion = 2;

            return data;
        }
        catch
        {
            var corruptBackup = Path.Combine(BackupFolder,
                $"ledger-corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            try { File.Copy(DataFile, corruptBackup, true); } catch { }
            var fresh = new LedgerData();
            fresh.Accounts.Add(new Account { Name = "现金", Balance = 0 });
            return fresh;
        }
    }

    public void Save(LedgerData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        Directory.CreateDirectory(BackupFolder);
        data.DataVersion = 2;

        var temp = DataFile + ".tmp";
        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(temp, json);

        if (File.Exists(DataFile))
        {
            var backup = Path.Combine(BackupFolder,
                $"ledger-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            try { File.Copy(DataFile, backup, true); } catch { }
        }

        File.Move(temp, DataFile, true);
    }

    public string CreateManualBackup()
    {
        Directory.CreateDirectory(BackupFolder);
        var target = Path.Combine(BackupFolder,
            $"manual-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        if (File.Exists(DataFile))
            File.Copy(DataFile, target, true);
        return target;
    }

    public void Restore(string file)
    {
        if (!File.Exists(file)) throw new FileNotFoundException("备份文件不存在。", file);
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        File.Copy(file, DataFile, true);
    }

    public string DataPath => DataFile;
    public string BackupPath => BackupFolder;
}

using System.Text.Json;

namespace LocalBookkeeper;

public sealed class StorageService
{
    private readonly string appDir;
    private readonly string dataFile;
    private readonly string backupDir;

    public StorageService()
    {
        appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LocalBookkeeper");

        dataFile = Path.Combine(appDir, "data", "ledger.json");
        backupDir = Path.Combine(appDir, "backups");

        Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!);
        Directory.CreateDirectory(backupDir);
    }

    public string DataFile => dataFile;

    public LedgerData Load()
    {
        try
        {
            if (!File.Exists(dataFile))
                return new LedgerData();

            var json = File.ReadAllText(dataFile);
            return JsonSerializer.Deserialize<LedgerData>(json) ?? new LedgerData();
        }
        catch
        {
            // Never overwrite damaged data. Start an empty in-memory view only.
            return new LedgerData();
        }
    }

    public void Save(LedgerData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!);

        var temp = dataFile + ".tmp";
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(temp, json);

        // Keep a rolling backup before replacing the main file.
        if (File.Exists(dataFile))
        {
            var backup = Path.Combine(
                backupDir,
                $"ledger-{DateTime.Now:yyyyMMdd-HHmmss}.json");

            try { File.Copy(dataFile, backup, true); } catch { }
        }

        File.Move(temp, dataFile, true);
        CleanupBackups();
    }

    public void CreateManualBackup()
    {
        if (!File.Exists(dataFile)) return;

        var target = Path.Combine(
            backupDir,
            $"manual-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");

        File.Copy(dataFile, target, true);
    }

    public bool RestoreFromFile(string source)
    {
        if (!File.Exists(source)) return false;

        var json = File.ReadAllText(source);
        var data = JsonSerializer.Deserialize<LedgerData>(json);

        if (data is null) return false;

        var temp = dataFile + ".restore.tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data,
            new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, dataFile, true);

        return true;
    }

    private void CleanupBackups()
    {
        try
        {
            var files = new DirectoryInfo(backupDir)
                .GetFiles("ledger-*.json")
                .OrderByDescending(x => x.CreationTimeUtc)
                .Skip(30);

            foreach (var f in files)
                f.Delete();
        }
        catch { }
    }
}

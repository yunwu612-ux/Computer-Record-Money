using System;
using System.IO;
using System.Linq;
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

            if (string.IsNullOrWhiteSpace(json))
                return new LedgerData();

            return JsonSerializer.Deserialize<LedgerData>(json)
                   ?? new LedgerData();
        }
        catch
        {
            // 不覆盖原始数据。
            // 如果数据文件损坏，只建立一个临时空账本视图。
            return new LedgerData();
        }
    }

    public void Save(LedgerData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!);
        Directory.CreateDirectory(backupDir);

        var tempFile = dataFile + ".tmp";

        var json = JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        // 先写临时文件，避免程序突然关闭时把正式账本写坏。
        File.WriteAllText(tempFile, json);

        // 保存正式文件之前，先留下历史备份。
        if (File.Exists(dataFile))
        {
            var backupFile = Path.Combine(
                backupDir,
                $"ledger-{DateTime.Now:yyyyMMdd-HHmmssfff}.json");

            try
            {
                File.Copy(dataFile, backupFile, true);
            }
            catch
            {
                // 备份失败不会阻止正常保存。
            }
        }

        // 原子替换正式数据文件。
        File.Move(tempFile, dataFile, true);

        CleanupBackups();
    }

    public void CreateManualBackup()
    {
        if (!File.Exists(dataFile))
            return;

        Directory.CreateDirectory(backupDir);

        var target = Path.Combine(
            backupDir,
            $"manual-backup-{DateTime.Now:yyyyMMdd-HHmmssfff}.json");

        File.Copy(dataFile, target, true);
    }

    public bool RestoreFromFile(string source)
    {
        if (!File.Exists(source))
            return false;

        try
        {
            var json = File.ReadAllText(source);

            if (string.IsNullOrWhiteSpace(json))
                return false;

            var restoredData =
                JsonSerializer.Deserialize<LedgerData>(json);

            if (restoredData is null)
                return false;

            Directory.CreateDirectory(Path.GetDirectoryName(dataFile)!);

            // 恢复之前先备份当前账本。
            if (File.Exists(dataFile))
            {
                var safetyBackup = Path.Combine(
                    backupDir,
                    $"before-restore-{DateTime.Now:yyyyMMdd-HHmmssfff}.json");

                try
                {
                    File.Copy(dataFile, safetyBackup, true);
                }
                catch
                {
                }
            }

            var tempFile = dataFile + ".restore.tmp";

            var restoredJson = JsonSerializer.Serialize(
                restoredData,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(tempFile, restoredJson);
            File.Move(tempFile, dataFile, true);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void CleanupBackups()
    {
        try
        {
            if (!Directory.Exists(backupDir))
                return;

            var files = new DirectoryInfo(backupDir)
                .GetFiles("ledger-*.json")
                .OrderByDescending(file => file.CreationTimeUtc)
                .Skip(30);

            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }
}

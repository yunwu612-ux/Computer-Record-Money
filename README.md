# 本地记账本 PC V1.0

一个简洁、本地优先的 Windows 记账应用。

## 核心特点
- 数据保存在用户数据目录，不放在程序安装目录
- 更新 EXE 不会覆盖账本数据
- 自动保存
- JSON 数据格式，便于长期保存和迁移
- 支持手动备份与恢复
- 收入 / 支出、分类、日期、备注
- 月度统计与账单搜索
- GitHub Actions 自动构建 Windows EXE

## 数据位置
默认：
`%APPDATA%\LocalBookkeeper\data\ledger.json`

备份目录：
`%APPDATA%\LocalBookkeeper\backups\`

因此即使替换新的 EXE，旧账本仍然保留。

## 本地开发
```powershell
dotnet restore
dotnet build
dotnet run --project src/LocalBookkeeper
```

## 发布
```powershell
dotnet publish src/LocalBookkeeper -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

发布后的程序不需要用户安装 .NET Runtime。

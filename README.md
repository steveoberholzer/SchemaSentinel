# SchemaSentinel

A WPF desktop tool for comparing SQL Server database schemas — tables, columns, procedures, views, and functions — side by side, with export to HTML, Markdown, JSON, and SQL ALTER scripts.

## Features

- **Side-by-side comparison** of any two SQL Server databases (on-prem, Azure SQL DB, Azure SQL MI)
- **Authentication support**: Windows Auth, SQL Server Auth, Microsoft Entra MFA (interactive browser)
- **Saved connection profiles** persisted to `%APPDATA%\SchemaSentinel\connections.json`
- **Collapsible connection panels** to free up screen space once connections are set
- **Logical mode** with configurable normalisation:
  - Ignore column order
  - Sort columns A–Z
  - Ignore whitespace (including collapsed multi-space runs like `CREATE   PROCEDURE`)
  - Ignore casing
  - Ignore SET statements (`SET ANSI_NULLS`, `SET QUOTED_IDENTIFIER`)
- **View filters** (no re-run required):
  - *Hide target-only objects* — suppress items that exist in Target but not Source
  - *Hide target-only columns* — ignore column diffs where the column only exists in Target
- **Clickable summary cards** — click Changed / Missing in Source / Missing in Target to jump directly to the first matching row
- **Exports**:
  - HTML report with diff detail
  - Markdown report
  - JSON (full result set)
  - SQL Script — `ALTER TABLE … ADD COLUMN` for missing columns, plus `CREATE` statements for missing procedures, views, and functions

## Projects

| Project | Purpose |
|---|---|
| `SchemaSentinel.Core` | Domain models, comparison engine, normalisation |
| `SchemaSentinel.Data` | SQL Server connectivity, metadata extraction, connection profile store |
| `SchemaSentinel.Reporting` | HTML, Markdown, JSON, and SQL script exporters |
| `SchemaSentinel.UI` | WPF application (MVVM) |

## Requirements

- Windows (WPF)
- .NET 8
- SQL Server, Azure SQL Database, or Azure SQL Managed Instance

## Getting Started

1. Clone the repo and open `SchemaSentinel.sln` in Visual Studio 2022+ or build with the .NET 8 SDK:
   ```
   dotnet build SchemaSentinel.sln
   dotnet run --project SchemaSentinel.UI
   ```
2. Enter Source and Target connection details. For Azure SQL MI with a public endpoint use the format:
   ```
   myinstance.public.<dns-zone>.database.windows.net,3342
   ```
3. Select **Entra MFA** authentication if you sign in via Microsoft Entra — a browser window will open for sign-in.
4. Click **Test Connection** on each panel to verify, then click **▶ Compare**.

## Authentication Notes

| Type | When to use |
|---|---|
| Windows Auth | On-prem or domain-joined machine inside the VNet |
| SQL Server Auth | Any environment with a SQL login |
| Entra MFA | Azure SQL with Azure AD enabled; opens a browser for interactive sign-in |

> Azure SQL MI public endpoints require `Authentication=Active Directory Interactive` and a device on a reachable network. Windows Auth does not work over the public endpoint.

## Comparison Modes

- **Logical** (default) — normalises whitespace, optional casing, strips SET statements; best for day-to-day drift detection
- **Strict** — byte-for-byte definition comparison

## SQL Script Export

The SQL Script export generates a `.sql` file containing:
- `ALTER TABLE [schema].[table] ADD [column] …` for every column present in Source but missing in Target
- Full `CREATE` statements for procedures, views, and functions present in Source but missing in Target
- A comment list of tables missing in Target (full `CREATE TABLE` generation is not included — use SSMS scripting for those)

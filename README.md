# UniversalConnectionTester

Small WPF app that loads a list of endpoints from `endpoints.json`, builds buttons for each, and lets you quickly test connectivity. Buttons turn green on success or red with an error dialog (selectable text). Certified 100% AI Slop.

## Prerequisites
- .NET 8 SDK
- Oracle Managed Data Access client (NuGet package is referenced; no client install needed)
- Network access to your targets

## Configure endpoints
Edit `endpoints.json` in the project root. Each entry needs:
- `name`: Display name for the button
- `connectionString`: URL or DB connection string
- `connectionType`: `mssql` | `oracle` | `http` | `https` | `ping`
- `ignoreSslErrors` (optional, bool): `true` to skip TLS validation (HTTP/HTTPS and MSSQL)

Example:
```json
{
  "endpoints": [
    {
      "name": "SQL Server Local",
      "connectionString": "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;",
      "connectionType": "mssql",
      "ignoreSslErrors": true
    },
    {
      "name": "Oracle HR",
      "connectionString": "User Id=user;Password=pass;Data Source=//db-host:1521/ORCLPDB1;",
      "connectionType": "oracle"
    },
    {
      "name": "HTTPS Status",
      "connectionString": "https://example.com/status",
      "connectionType": "https",
      "ignoreSslErrors": true
    },
    {
      "name": "Ping Gateway",
      "connectionString": "8.8.8.8",
      "connectionType": "ping"
    }
  ]
}
```

## Run (debug)
```powershell
dotnet run
```
Build outputs are ignored by Git (`bin/`, `obj/`).

## Publish a single EXE
Project is set for single-file, self-contained publish (`win-x64`). Produce the exe:
```powershell
dotnet publish -c Release
```
Find the output in `bin\Release\net8.0-windows\win-x64\publish\UniversalConnectionTester.exe`.

## How it works
- On load, the app reads `endpoints.json` (copied to output).
- Each endpoint becomes a button.
- On click, it runs the appropriate test:
  - MSSQL: `SqlConnection` with 10s timeout; `TrustServerCertificate` if `ignoreSslErrors` is true.
  - Oracle: `OracleConnection` with 10s timeout.
  - HTTP/HTTPS: `HttpClient`; if `ignoreSslErrors` true, uses a handler that accepts any cert.
  - Ping: ICMP ping (3s).
- Errors show in a scrollable dialog with selectable text.
 
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
- `connectionType`: `mssql` | `oracle` | `redis` | `http` | `https` | `ping`
- `ignoreSslErrors` (optional, bool): `true` to skip TLS validation (HTTP/HTTPS, MSSQL, and Redis TLS)

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
      "name": "Redis Cache",
      "connectionString": "localhost:6379,abortConnect=false",
      "connectionType": "redis"
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
Project is set for single-file, self-contained publish (`win-x64`). Produce the local publish output with:
```powershell
dotnet publish -c Release
```
Find the output in `bin\Release\net8.0-windows\win-x64\publish\UniversalConnectionTester.exe`.
The app still expects `endpoints.json` beside the exe.

## GitHub Actions
Pushing a tag triggers [`.github/workflows/publish-on-tag.yml`](.github/workflows/publish-on-tag.yml).
- Builds a self-contained `win-x64` publish on `windows-latest`
- Creates a versioned package named `UniversalConnectionTester-<tag>-win-x64.zip`
- Includes `UniversalConnectionTester-<tag>-win-x64.exe` and `endpoints.json`
- Uploads the zip as both a workflow artifact and a GitHub release asset

## How it works
- On load, the app reads `endpoints.json` (copied to output).
- Each endpoint becomes a button.
- On click, it runs the appropriate test:
  - MSSQL: `SqlConnection` with 10s timeout; `TrustServerCertificate` if `ignoreSslErrors` is true.
  - Oracle: `OracleConnection` with 10s timeout.
  - Redis: `StackExchange.Redis` `ConnectionMultiplexer` with 10s connect/command timeouts, plus `PING`.
  - HTTP/HTTPS: `HttpClient`; if `ignoreSslErrors` true, uses a handler that accepts any cert.
  - Ping: ICMP ping (3s).
- Errors show in a scrollable dialog with selectable text.
 

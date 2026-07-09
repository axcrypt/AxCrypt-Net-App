# Building AxCrypt

## Prerequisites

### .NET 10 SDK

Install the .NET 10 SDK for your platform from
https://dotnet.microsoft.com/download/dotnet/10.0 :

- **Windows**: `winget install Microsoft.DotNet.SDK.10`
- **macOS**: `brew install --cask dotnet-sdk` (or the official installer)
- **Linux**: distribution packages or
  `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0`

Verify: `dotnet --version` prints `10.x`.

### Windows desktop app only

- Windows 10 (19041+) or Windows 11
- MAUI workload: `dotnet workload install maui`
- Visual Studio 2026 (any edition) is convenient but not required.

## Restore, build, test

```bash
# Cross-platform (Windows, macOS, Linux): CLI and shared libraries
dotnet restore src/AxCrypt.Cli/AxCrypt.Cli.csproj
dotnet build   src/AxCrypt.Cli/AxCrypt.Cli.csproj -c Release

# Run the cross-platform tests
dotnet test tests/AxCrypt.Cli.Test/AxCrypt.Cli.Test.csproj  -c Release
dotnet test tests/AxCrypt.Core.Test/AxCrypt.Core.Test.csproj -c Release
dotnet test tests/AxCrypt.Common.Test/AxCrypt.Common.Test.csproj -c Release
dotnet test tests/AxCrypt.Mono.Test/AxCrypt.Mono.Test.csproj -c Release

# Windows only: everything, including the desktop app
dotnet build src/AxCrypt.Net.App.sln -c Release
```

## Publishing the CLI

```bash
# Framework-dependent, single folder
dotnet publish src/AxCrypt.Cli/AxCrypt.Cli.csproj -c Release -o publish/cli

# Self-contained single file per platform
dotnet publish src/AxCrypt.Cli/AxCrypt.Cli.csproj -c Release -r win-x64   --self-contained -p:PublishSingleFile=true -o publish/win-x64
dotnet publish src/AxCrypt.Cli/AxCrypt.Cli.csproj -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true -o publish/osx-arm64
dotnet publish src/AxCrypt.Cli/AxCrypt.Cli.csproj -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64
```

The binary is named `axcrypt` (`axcrypt.exe` on Windows).

## Platform-specific notes

- **Linux/macOS**: the desktop app (`src/AxCrypt.App.Windows`) is
  Windows-only and is conditionally excluded from non-Windows builds; use
  the CLI project or individual library projects instead of the full
  solution.
- **CLI settings**: the CLI stores non-secret settings (e.g. calibrated
  key-wrap iteration counts) under the per-user application data folder;
  override with the `AXCRYPT_CLI_WORKFOLDER` environment variable.
- **Cloud drive integrations** (desktop app): Dropbox/Google
  Drive/OneDrive OAuth credentials are not committed. Without them the
  app builds and runs; only cloud sign-in is disabled. Supply values via
  environment variables or a `CloudDriveSecrets.BuildTime.cs` file — see
  `src/AxCrypt.App.Shared/CloudCore/CloudDriveSecrets.cs` and the
  `.template` file next to it.
- **Legacy projects**: `shared/AxCrypt.Reports*` are legacy and not part
  of the solution; they are retained for reference only.

## Formatting and analysis

```bash
dotnet format --verify-no-changes   # style check (CI enforces this on changed projects)
```

## Signing (placeholders — official releases only)

Community builds are unsigned; that is expected and fine. Official
AxCrypt AB builds are signed in CI using protected secrets:

| Purpose | Variable / secret | Notes |
|---|---|---|
| Windows Authenticode / MSIX | `AX_SIGNING_CERT_THUMBPRINT` | Thumbprint of a cert in the CI machine store |
| signtool location override | `AX_SIGNTOOL_PATH` | Defaults to `signtool.exe` on `PATH` |
| Timestamp server | `AX_TIMESTAMP_URL` | Defaults to a public RFC 3161 server |
| macOS / Android / iOS | see [docs/SIGNING.md](docs/SIGNING.md) | |

No certificates, private keys, or provisioning profiles exist in this
repository. See [docs/SIGNING.md](docs/SIGNING.md) for the complete
signing documentation.

# AxCrypt

[![License: GPL v3](https://img.shields.io/badge/License-GPL_v3-48772C.svg)](LICENSE)
[![CI](https://img.shields.io/badge/CI-GitHub_Actions-48772C.svg)](.github/workflows/ci.yml)
[![Platform](https://img.shields.io/badge/.NET-10-48772C.svg)](https://dotnet.microsoft.com/)

AxCrypt is file encryption software, developed and maintained by
[AxCrypt AB](https://axcrypt.net). It encrypts files with AES-256 in the
open, documented `.axx` file format, supports password-based and
public-key ("key sharing") encryption, and ships as a desktop app and a
cross-platform command-line utility.

**Copyright (C) AxCrypt AB.** Source code licensed under
[GPL-3.0-or-later](LICENSE). "AxCrypt" and the AxCrypt logo are trademarks
of AxCrypt AB — see [TRADEMARK.md](TRADEMARK.md) and [NOTICE](NOTICE).

## Project status

Active development. This repository contains the .NET 10 code base:
the shared encryption core, the Windows MAUI Blazor Hybrid desktop app,
and the `axcrypt` command-line utility.

## Features

- AES-256 file encryption (AxCrypt V2 format), with AES-128 V1 `.axx`
  backward compatibility for decryption
- Key sharing: encrypt to one or more recipients' RSA public keys
- Compression before encryption
- Integrity protection (HMAC) — tampering is detected
- Cross-platform command-line utility (`axcrypt`) for Windows, macOS, Linux
- Desktop application for Windows (MAUI Blazor Hybrid)

## Repository layout

| Path | Contents |
|---|---|
| `shared/` | Core libraries: crypto core (`AxCrypt.Core`), abstractions, platform implementations (`AxCrypt.Mono`, `AxCrypt.Desktop`), adapted Bouncy Castle |
| `src/AxCrypt.App.Windows` | Windows desktop app (MAUI Blazor Hybrid) |
| `src/AxCrypt.Cli` | Command-line utility (`axcrypt`) |
| `tests/` | NUnit test projects, including CLI round-trip tests |
| `docs/` | CLI reference, code signing documentation |

## Build requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- For the Windows desktop app only: Windows 10/11 with the MAUI workload
  (`dotnet workload install maui`)

The core libraries, CLI, and tests build and run on Windows, macOS, and
Linux. **No AxCrypt AB account, server, or private infrastructure is
required** to build or use the open-source core and CLI.

## How to build

```bash
git clone <this-repository>
cd AxCrypt-Net-App

# Cross-platform: CLI + core + tests
dotnet build src/AxCrypt.Cli/AxCrypt.Cli.csproj -c Release

# Windows only: full solution including desktop app
dotnet build src/AxCrypt.Net.App.sln -c Release
```

See [BUILDING.md](BUILDING.md) for details and platform-specific notes.

## How to run

Desktop app (Windows): `dotnet run --project src/AxCrypt.App.Windows`, or
launch the built executable.

CLI (all platforms):

```bash
dotnet run --project src/AxCrypt.Cli -- help
# or after `dotnet publish`: ./axcrypt help
```

## Command-line quick start

```bash
# Windows (PowerShell), macOS and Linux (bash) — identical syntax:

# Encrypt (prompts for password without echoing it)
axcrypt encrypt --input file.txt --output file.txt.axx

# Decrypt
axcrypt decrypt --input file.txt.axx --output file.txt

# Automation-friendly password input (avoids shell history):
export AXCRYPT_PASSWORD='your-password'      # macOS/Linux
$env:AXCRYPT_PASSWORD = 'your-password'      # Windows PowerShell
axcrypt encrypt --input report.pdf

# Generate an RSA key pair for key sharing
axcrypt keygen --email you@example.com --output ./keys

# Encrypt directly to a recipient's public key
axcrypt encrypt --input file.txt --recipient-public-key ./keys/you@example.com-public.json

# Add a recipient to an existing encrypted file
axcrypt recipients add --file file.txt.axx --public-key recipient-public.json

# Inspect an encrypted file (name, timestamps, recipients)
axcrypt show --input file.txt.axx

axcrypt version
axcrypt help
```

Full reference, exit codes, and automation guidance: [docs/CLI.md](docs/CLI.md).

## Security model

- Files are encrypted with AES (256-bit in the V2 format) using keys
  derived from your password (PBKDF2/NIST key wrap with calibrated
  iterations) and/or wrapped with recipients' RSA public keys.
- Integrity is protected with an HMAC; tampering and corruption are
  detected on decryption.
- **Your password is the security boundary.** Anyone with the password (or
  a shared private key) can decrypt; a lost password cannot be recovered.
- The CLI never logs or echoes passwords, keys, or plaintext, and works
  fully offline.
- Cryptographic primitives come from the shared `AxCrypt.Core` /
  Bouncy Castle code; the CLI and apps contain no crypto implementations
  of their own.

Found a vulnerability? Please report it privately — see [SECURITY.md](SECURITY.md).

## License

GPL-3.0-or-later. You may use, study, modify, and redistribute this code,
provided derivative works are also licensed under the GPL and retain
copyright notices. The full text is in [LICENSE](LICENSE). Third-party
components and their licenses are listed in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

The GPL applies to the source code only — **not** to the AxCrypt name,
logo, signing certificates, or AxCrypt AB services. See
[TRADEMARK.md](TRADEMARK.md).

## Official signed builds

Only builds digitally signed by AxCrypt AB are official AxCrypt releases.
Community builds are welcome but must not present themselves as official.
How official builds are signed, and how to verify signatures and release
checksums, is documented in [docs/SIGNING.md](docs/SIGNING.md) and
[RELEASING.md](RELEASING.md).

## Contributing

Contributions are welcome — please read [CONTRIBUTING.md](CONTRIBUTING.md)
(including the special review rules for security-sensitive and crypto
code) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Support questions:
[SUPPORT.md](SUPPORT.md).

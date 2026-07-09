# Code Signing — Official AxCrypt AB Releases

Official AxCrypt releases are digitally signed by AxCrypt AB. **No
certificates, private keys, keystores, or provisioning profiles are stored
in this repository** — signing material lives exclusively in protected
CI/CD secret storage (GitHub Actions environments/secrets or an HSM/cloud
key vault). Community builds are unsigned; they work identically but must
not be presented as official (see ../TRADEMARK.md).

## Windows (Authenticode / MSIX)

The MSIX signing hook is in `src/SolutionItems/AxCrypt.Net.App.Build.targets`.
Signing is skipped entirely when no thumbprint is configured.

| Setting | Source | Purpose |
|---|---|---|
| `AX_SIGNING_CERT_THUMBPRINT` | CI secret / env | SHA-1 thumbprint of the AxCrypt AB certificate in the machine certificate store |
| `AX_SIGNTOOL_PATH` | env (optional) | Full path to `signtool.exe`; defaults to `signtool.exe` on `PATH` (Windows SDK) |
| `AX_TIMESTAMP_URL` | env (optional) | RFC 3161 timestamp server; defaults to a public server |

CI flow (official): import the PFX from the secret store into the
ephemeral runner's certificate store (or use an Azure Key Vault /
Trusted Signing task), set `AX_SIGNING_CERT_THUMBPRINT`, build, then
`signtool verify /pa` as a post-step.

Verification by users: `signtool verify /pa AxCrypt-<version>.msix` or the
file's Digital Signatures tab — signer must be **AxCrypt AB**.

## macOS (Developer ID + notarization)

Required CI secrets (never committed):

| Secret | Purpose |
|---|---|
| `APPLE_DEVELOPER_ID_APPLICATION_P12` (base64) + `APPLE_P12_PASSWORD` | Developer ID Application certificate |
| `APPLE_ID`, `APPLE_TEAM_ID`, `APPLE_APP_SPECIFIC_PASSWORD` | Notarization credentials for `notarytool` |

CI flow: create an ephemeral keychain → import the P12 →
`codesign --timestamp --options runtime` all binaries →
`xcrun notarytool submit --wait` → `xcrun stapler staple`.

Verification by users: `codesign -dv --verbose=2` (Developer ID: AxCrypt
AB) and `spctl -a -vv` (accepted, notarized).

## Android

| Secret | Purpose |
|---|---|
| `ANDROID_KEYSTORE` (base64 `.keystore`/`.jks`) | Release keystore |
| `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD` | Keystore/key credentials |

CI flow: decode the keystore to the runner's temp dir, pass
`-p:AndroidKeyStore=true -p:AndroidSigningKeyStore=... -p:AndroidSigningKeyAlias=...`
to `dotnet publish`, delete the keystore afterwards.

Verification by users: `apksigner verify --print-certs` — the certificate
fingerprint is published in the release notes.

## iOS

| Secret | Purpose |
|---|---|
| `IOS_DISTRIBUTION_P12` (base64) + `IOS_P12_PASSWORD` | Apple Distribution certificate |
| `IOS_PROVISIONING_PROFILE` (base64) | Distribution provisioning profile |
| App Store Connect API key (`ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_P8`) | Upload/automation |

CI flow: ephemeral keychain + profile install →
`dotnet publish -f net10.0-ios -p:ArchiveOnBuild=true` with signing
properties → upload via `altool`/Transporter or the ASC API.

## Rules

1. Signing secrets exist only in protected CI environments with required
   reviewers; never in the repository, issues, or logs.
2. Ephemeral runners must delete imported keys/keychains in an `always()`
   cleanup step.
3. Community forks: leave all signing variables unset — builds succeed
   unsigned. Do not sign forks in a way that implies AxCrypt AB origin.
4. Rotation: on any suspicion of compromise, revoke the certificate,
   rotate secrets, and publish an advisory.

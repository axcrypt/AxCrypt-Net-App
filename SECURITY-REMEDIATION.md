# Security & Cleanup Remediation — pre-open-source

Branch: `shrd-net-sync` · Baseline commit: `0a581908` · Prepared 2026-07-22

This tracks remediation of the prioritized review findings. Part 1 lists fixes
already applied to the working tree (review the diff before committing). Part 2
lists actions that only you can perform (they need credentials, are destructive
to git history, or require file deletion/rename that the review tooling could not
do in this environment).

---

## Part 1 — Fixes applied to source (ready to review & commit)

| Finding | Change | File |
|---|---|---|
| **H1** – Global TLS-validation bypass | `RuntimeEnvironment.DebugMode` no longer sets `ServerCertificateValidationCallback => true`; it is now a documented no-op for certificate handling. Debug verbosity is still driven by the log level elsewhere. | `shared/AxCrypt.Mono/RuntimeEnvironment.cs` (~L208) |
| **M2** – Cleartext HTTP for FX data | Three ECB URLs switched to `https://` and normalized to `www.ecb.europa.eu`. | `shared/AxCrypt.International.WebServices/EcbExchangeService.cs` (L90, L94, L96) |
| **M10** – Wrong cloud upload chunk size | Typo `* 1024 * 102` → `* 1024 * 1024` (8 MB / 16 MB). The old value (835,584 B) is not a multiple of 256 KB and is rejected by Google Drive resumable uploads. | `.../CloudCore/GoogleDrive/GoogleDriveConfiguration.cs` and `.../CloudCore/DropBox/DropBoxConfiguration.cs` (L25–L27) |
| **M7** – `throw ex;` loses stack trace | Changed to `throw;` (only remaining occurrence in app/shared code). | `src/AxCrypt.App.Shared/CloudCore/FileStorageProvider.cs` (L71) |

**C1 (hardcoded cloud secrets) is already resolved in source** — credentials now
resolve through `CloudDriveSecrets.Get(...)` (env var → git-ignored build-time
partial → empty). No live secret strings remain in the tree. The history cleanup
below is still required.

Suggested verification once a .NET SDK is available:

```bash
dotnet build src/AxCrypt.Net.App.sln -c Release
dotnet test tests/AxCrypt.Core.Test tests/AxCrypt.Common.Test
```

> Note: `RuntimeEnvironment.cs` may now have unused `using`s (e.g. cert/SSL types).
> Harmless, but remove them if your build treats warnings as errors.

---

## Part 1b — Crypto & file-path audit fixes applied to source

From the focused crypto/file-path audit (see `AxCrypt-Net-App_Crypto-File-Audit_2026-07-22.docx`).
All are unstaged edits; **none could be compiled here — build and run the crypto tests before merging.**

| Finding | Change | File |
|---|---|---|
| **CR-1** – Non-constant-time MAC compare | `Hmac.operator ==` now uses `CryptographicOperations.FixedTimeEquals` instead of the early-exit `IsEquivalentTo`. Flows through `DecryptTo` and `VerifyHmac`. | `shared/AxCrypt.Core/Crypto/Hmac.cs` (~L129) |
| **CR-2** – Unverified plaintext written to destination | `Decrypt(document, FileLock, …)` now decrypts into a `.tmp`, and only moves it into place after `DecryptTo` returns (it throws on HMAC mismatch). On failure the temp is wiped, not the destination. Reuses `MakeAlternatePath` / `MoveTemporaryToDestinationWithBackupAndWipe`. | `shared/AxCrypt.Core/AxCryptFile.cs` (`Decrypt`, ~L705) |
| **CR-3** – Weak/low KDF work factor | PBKDF2-HMAC-SHA512 iterations raised `1000 → 210000`; key-wrap calibration target `~1/20 s → ~1/2 s` (`/20 → /2`) and floor `5000 → 20000`; calibration warm-up moved out of the timed window (also fixes CR-5). | `shared/AxCrypt.Core/Crypto/CryptoFactory.cs` (L56) and `.../Crypto/IterationCalculator.cs` (L64–L90) |
| **CR-4** – Biased random passwords | `GenerateRandomPassword` uses `RandomNumberGenerator.GetItems<char>(…)` (uniform) instead of `% alphabetLength`. | `shared/AxCrypt.Core/AxCryptFile.cs` (`GenerateRandomPassword`) |

**Compatibility notes for CR-2 and CR-3 (read before committing):**

- **CR-2** reorders a core decrypt path. Run `tests/AxCrypt.Core.Test` and `tests/AxCrypt.Common.Test`; watch for tests that assert decryption writes directly to the destination or that inspect `.tmp`/`.bak` behavior. The stream-to-stream `Decrypt` overloads and the intentional `TryDecryptBrokenFile` recovery path were left unchanged by design.
- **CR-3 does not make any existing file unreadable.** Derivation salt/iterations and key-wrap iterations are stored per file and read back on decrypt, so old files open with their own stored values and new files remain readable by older builds. The one upgrade side effect: the local passphrase thumbprint (`SymmetricKeyThumbprint`) is computed from the `DerivationIterations` constant, so cached "recent files" key associations may require a single re-login after upgrade. Consider a one-line release note. Higher iteration counts also add a few hundred ms to login on slower/mobile devices — verify UX on a low-end target.
- **`.tmp` scanning:** since CR-2 now writes plaintext to a `.tmp` beside the destination, make sure `*.tmp` in user data directories is excluded from any bundled logging/telemetry and is covered by the "known limitations" wipe caveat (see `SECURITY.md`).

---

## Part 2 — Actions only you can perform

### A. Rotate the leaked cloud credentials (do this FIRST — highest priority)
The Google client secret (`GOCSPX-…`) and Dropbox app secret (`enma8m952i35ojh`)
were committed previously and must be treated as compromised even after history is
purged.

1. Google Cloud Console → APIs & Services → Credentials → reset the OAuth client secret.
2. Dropbox App Console → your app → regenerate the App secret.
3. Store the new values as CI secrets / local env vars using the names in
   `CloudDriveSecrets.cs` (`AXCRYPT_GOOGLE_CLIENT_SECRET_DESKTOP`,
   `AXCRYPT_DROPBOX_APP_SECRET`, etc.). Never commit them.

### B. Purge the secrets from git history (before making the repo public)
They still exist in commits `72f82b9b`, `cb5cd462`, `e4a56d25`. Deleting from HEAD
is not enough. Rotate (step A) first, then rewrite history.

Recommended: git-filter-repo (create `secrets.txt` with one literal per line):

```bash
# secrets.txt
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
enma8m952i35ojh
omrx7hccdskf45r

# run from a FRESH clone
pip install git-filter-repo
git filter-repo --replace-text secrets.txt   # replaces each with ***REMOVED***
git push --force --all && git push --force --tags
```

Or with BFG:

```bash
bfg --replace-text secrets.txt
git reflog expire --expire=now --all && git gc --prune=now --aggressive
git push --force --all
```

After the force-push, every collaborator must re-clone; existing forks/PRs may still
carry the old blobs, so verify none remain public.

### C. Add secret scanning (prevent regressions)
- Enable GitHub secret scanning + push protection on the repo.
- Add a `gitleaks` pre-commit hook / CI job.

### D. Delete dead stub files (L2) — could not be removed here (filesystem read-only for delete)
All are 3–7 line retired stubs:

```bash
git rm \
  src/AxCrypt.App.Entitlement/IEntitlementApiClient.cs \
  src/AxCrypt.App.Entitlement/IEntitlementCache.cs \
  src/AxCrypt.App.Entitlement/EntitlementSnapshot.cs \
  src/AxCrypt.App.Entitlement/EntitlementServiceCollectionExtensions.cs \
  src/AxCrypt.App.Entitlement/HttpEntitlementApiClient.cs \
  src/AxCrypt.App.Entitlement/EntitlementService.cs \
  src/AxCrypt.App.Entitlement/FileEntitlementCache.cs \
  src/AxCrypt.App.Shared/Models/SignUpFrom.cs \
  src/AxCrypt.App.Shared/Entitlement/IEntitlementService.cs \
  src/AxCrypt.App.Shared/Entitlement/FeatureKey.cs \
  src/AxCrypt.App.Shared/Entitlement/FeatureUsage.cs \
  src/AxCrypt.App.Shared/Entitlement/IFeatureUsageProvider.cs
```

Build afterward to confirm nothing referenced them (they are retired shims, so it
should be clean).

### E. Rename files with a space in the name (L4) — could not be renamed here
```bash
git mv "src/AxCrypt.App.Shared/ViewModels/AccountStatusViewModel .cs" \
       "src/AxCrypt.App.Shared/ViewModels/AccountStatusViewModel.cs"
git mv "src/AxCrypt.App.Shared/Password/SecretClientCollection .cs" \
       "src/AxCrypt.App.Shared/Password/SecretClientCollection.cs"
```

### F. Not attempted (need a build to do safely) — schedule separately
Left untouched because they are large refactors that cannot be verified without
compiling: retarget the remaining `net6.0` projects to `net10.0` (H2), pin the
floating `4.8.*` package versions (M4), migrate the `New<T>()` service locator to
DI (M5), split the god classes (M6), and convert `async void` cloud/OAuth flows to
`async Task` (M8). See the full review document for details.

---

## Suggested commit sequence
1. Review `git diff` for the Part 1 edits; commit as *"Security: remove TLS bypass, fix ECB https, chunk size, rethrow"*.
2. Do D and E; commit as *"Cleanup: remove retired entitlement stubs, fix filenames"*.
3. Rotate secrets (A), then purge history (B) from a fresh clone, then enable scanning (C).

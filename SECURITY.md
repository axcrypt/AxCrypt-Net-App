# Security Policy

AxCrypt is security software; we take vulnerability reports seriously and
appreciate responsible disclosure.

## How to report a vulnerability (privately)

**Do not open a public GitHub issue for security vulnerabilities.**

Preferred channels:

1. **GitHub private vulnerability reporting**: use the repository's
   *Security* tab → *Report a vulnerability* (GitHub Security Advisories).
2. **E-mail**: security@axcrypt.net. If you need to send sensitive
   details, request our PGP key in a first e-mail, or attach an
   `.axx`-encrypted file and share the password out of band.

Please include: affected version/commit, platform, reproduction steps or a
proof of concept, and your assessment of impact.

## What to expect (responsible disclosure process)

- **Acknowledgement** of your report within 3 business days.
- **Initial assessment** within 10 business days.
- We will keep you informed of progress and agree on a coordinated
  disclosure date — normally within 90 days of the report, earlier when a
  fix ships sooner.
- We will credit reporters in release notes unless you prefer otherwise.
- We do not pursue legal action against good-faith security research that
  respects user privacy and does not disrupt production services.

## Supported versions

| Version | Supported |
|---|---|
| Latest release (current major) | ✔ Security fixes |
| Older releases | ✖ Please upgrade |

Only builds signed by AxCrypt AB are official; community builds are the
responsibility of their distributors.

## Scope notes

- In scope: this repository's code — encryption core, file format
  handling, key management, desktop app, CLI, build/release pipeline.
- The password chosen by the user is the primary security boundary;
  weak-password brute force against a user's own file is not a
  vulnerability in itself.
- AxCrypt AB online services (`*.axcrypt.net`) are out of scope for this
  repository; report service issues to security@axcrypt.net as well.

## Known limitations

AxCrypt provides strong file encryption, but some properties depend on the
operating system and storage hardware and **cannot be guaranteed by the
application alone**. These are documented so users can make informed decisions
rather than rely on assumptions. None of the items below is treated as a
vulnerability on its own.

### 1. "Wipe" / secure delete is best-effort, not guaranteed on modern storage

When AxCrypt wipes a file (the original plaintext after encryption, or a temporary
file), it overwrites the file's data once and deletes it. **On much modern storage
this does not reliably destroy the original bytes**, because:

- **SSDs and other flash** use wear-leveling and over-provisioning, so a logical
  overwrite is often redirected to a different physical page, leaving the original
  data in a block the OS can no longer address.
- **Copy-on-write / journaling filesystems** (APFS, Btrfs, ZFS, ext4 with a journal,
  ReFS) may retain previous versions of the data.
- **Snapshots, Time Machine / VSS, and backups** may hold earlier copies.
- **Cloud-synced folders** (OneDrive, Dropbox, Google Drive, iCloud) usually keep
  server-side version history a local overwrite cannot touch.
- **RAID, thin-provisioned, and network volumes** may not overwrite in place.

No number of overwrite passes fixes this on flash. Treat "wipe" as a convenience that
removes the file and reduces casual recoverability — **not** as defensible
sanitization.

If you need data to be unrecoverable: prefer **full-disk/volume encryption**
(BitLocker, FileVault, LUKS) so that destroying the key renders residual data
unreadable; on SSDs use the drive's **ATA Secure Erase / TRIM** or cryptographic-erase
via OS tools; and ideally avoid writing plaintext to disk at all (see item 3).

### 2. Password-based work factor is bounded by the encrypting device

Keys are derived from your passphrase with PBKDF2-HMAC-SHA512 plus a device-calibrated
AES key-wrap step. Calibration targets a fixed amount of time on the machine doing the
encryption, so **files encrypted on a slower device get a lower iteration count** than
the same passphrase on a fast machine. The work factor and salt are stored per file, so
files stay decryptable across devices, but the brute-force cost is only as high as the
encrypting device allowed. Use a strong, high-entropy passphrase. A future file-format
revision is expected to adopt a memory-hard KDF (Argon2id).

### 3. Plaintext may exist transiently on disk during decryption

Decrypting a file writes its contents to disk — to a temporary file that is moved into
place, and/or to the destination you choose. During that window the plaintext exists
unencrypted and is subject to the same wipe limitations as item 1 (indexers, antivirus,
backup agents and cloud sync may also read it). For maximum protection, decrypt only on
trusted, encrypted volumes and remove decrypted files when finished.

### 4. Legacy (v1) archives use deprecated primitives

For backward compatibility AxCrypt can still open older **version 1** archives, which
use SHA-1-based constructions. These are used **only to read legacy files**; new
encryption always uses the current version-2 format. Re-encrypt important v1 files to
the current format.

*This list describes current, known limitations; it is not a claim that no other issues
exist. Please report anything you find via the process above.*

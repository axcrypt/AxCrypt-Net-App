# Contributing to AxCrypt

Thank you for your interest in contributing. This project is Copyright (C)
AxCrypt AB and licensed GPL-3.0-or-later; by contributing you agree that
your contributions are licensed under the same terms.

## Branching workflow

- `master` is the integration branch and must always build and pass tests.
- Create feature branches from `master`: `feature/<short-description>`,
  `fix/<short-description>`, or `docs/<short-description>`.
- Releases are tagged `v<major>.<minor>.<patch>` from `master`.
- Keep branches small and focused; rebase on `master` before opening a PR.

## Coding standards

- C# with .NET 10 SDK-style projects; `Nullable` and `ImplicitUsings`
  enabled for new projects.
- Follow the existing style of the file you are editing. Do not reformat
  code unrelated to your change.
- New source files must start with the SPDX header:

  ```csharp
  // SPDX-License-Identifier: GPL-3.0-or-later
  // Copyright (C) AxCrypt AB
  ```

- No new NuGet dependencies without prior discussion in an issue. Any new
  dependency must be GPL-v3-compatible and added to
  `THIRD-PARTY-NOTICES.md`.
- Run `dotnet format` before committing; CI verifies formatting.

## Pull request process

1. Open or reference an issue describing the problem or feature.
2. Ensure `dotnet build` and `dotnet test` pass locally (see BUILDING.md).
3. Fill in the pull request template, including a description of testing
   performed.
4. At least one maintainer review is required. CI (build, tests, format,
   secret scan, dependency review) must be green.
5. Maintainers merge with a squash or rebase merge; no merge commits.

## Test requirements

- New functionality requires tests (NUnit). Bug fixes require a regression
  test that fails before the fix.
- Encryption/decryption behavior changes require round-trip tests,
  including failure cases (wrong password, corrupted input).
- Tests must be cross-platform: no hard-coded path separators, no
  Windows-only APIs in cross-platform test projects.

## Security-sensitive code review rules

Changes touching any of the following require review by **two**
maintainers, at least one designated as a security reviewer:

- `shared/AxCrypt.Core/Crypto/**`, `shared/AxCrypt.Core/Header/**`,
  `shared/AxCrypt.Core/Reader/**`, `shared/AxCrypt.Core/Secrets/**`
- `shared/BouncyCastle.AxCrypt/**`
- Key derivation, random number generation, key wrap iteration logic
- Password handling anywhere (including the CLI)
- The `.axx` file format

Rules:

- Never log or persist passwords, derived keys, or plaintext.
- Never weaken defaults (key sizes, iteration counts, algorithms), even
  temporarily, without an approved design issue.
- Constant-time comparison for authentication tags and thumbprints must
  not be replaced with ordinary equality.

## Crypto change approval process

1. Open an issue labeled `crypto` describing the motivation, the exact
   change, and its compatibility impact on existing `.axx` files.
2. A maintainer with crypto responsibility must approve the design
   *before* implementation.
3. The PR must include: round-trip tests, backward-compatibility tests
   against existing test data files, and an updated format description if
   the file format changes.
4. No changes to crypto behavior are accepted as drive-by refactorings.

## Trademark reminder

Contributions become part of a GPL project, but "AxCrypt" and the AxCrypt
logo remain trademarks of AxCrypt AB. See TRADEMARK.md.

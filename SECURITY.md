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

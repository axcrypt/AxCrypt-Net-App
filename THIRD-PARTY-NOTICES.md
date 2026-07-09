# Third-Party Notices

AxCrypt — Copyright (C) AxCrypt AB — is licensed under GPL-3.0-or-later
(see [LICENSE](LICENSE)). It depends on the third-party components below.
All listed licenses are compatible with GPL v3 (MIT, BSD-3-Clause, and
Apache-2.0 are one-way compatible with GPL-3.0-or-later).

License texts are available from the linked projects and from
https://spdx.org/licenses/. Where a license requires reproduction of its
notice, the notice is included with the corresponding NuGet package.

## Embedded source

| Component | License | Notes |
|---|---|---|
| Bouncy Castle (adapted fork, `shared/BouncyCastle.AxCrypt`) | MIT (Bouncy Castle License) | Copyright (c) 2000-present The Legion of the Bouncy Castle Inc. — https://www.bouncycastle.org. Includes a C# adaptation of the bzip2 compression code. |

## NuGet packages — applications and libraries

| Package | Version | License |
|---|---|---|
| BouncyCastle.Cryptography | 2.6.1 | MIT (Bouncy Castle License) |
| Newtonsoft.Json | 13.0.2 / 13.0.3 | MIT |
| Azure.Core | 1.51.1 | MIT |
| Dropbox.Api | 7.0.0 | MIT |
| Google.Apis | 1.70.0 | Apache-2.0 |
| Google.Apis.Drive.v3 | 1.70.0.3834 | Apache-2.0 |
| Microsoft.AspNetCore.Components.Web | 9.0.7 | MIT |
| Microsoft.AspNetCore.Components.WebView.Maui | 9.0.90 | MIT |
| Microsoft.AspNetCore.DataProtection.Abstractions | 10.0.8 | MIT |
| Microsoft.Extensions.ApiDescription.Client | 3.0.0 | MIT |
| Microsoft.Extensions.Caching.Memory | 9.0.7 | MIT |
| Microsoft.Extensions.Logging.Debug | 9.0.7 | MIT |
| Microsoft.Graph | 5.86.0 | MIT |
| Microsoft.Identity.Client | 4.74.0 | MIT |
| Microsoft.Kiota.Abstractions | 1.22.0 | MIT |
| Microsoft.Maui.Controls (+ Compatibility) | 9.0.90 | MIT |
| Microsoft.SourceLink.GitHub | 8.0.0 | MIT |
| Microsoft.Toolkit.Uwp.Notifications | 7.1.3 | MIT |
| Microsoft.Win32.SystemEvents | 9.0.7 | MIT |
| Nerdbank.GitVersioning | 3.7.115 | MIT |
| NLight | 2.1.1 | MIT |
| NSwag.ApiDescription.Client | 13.0.5 | MIT |
| PInvoke.User32 | 0.7.124 | MIT |
| System.Drawing.Common | 9.0.7 | MIT |
| System.Runtime.Caching | 9.0.7 | MIT |
| System.Security.Cryptography.ProtectedData | 9.0.9 | MIT |
| System.Security.Cryptography.Xml | 9.0.9 / 9.0.15 | MIT |
| System.ServiceModel.* (Duplex, Federation, Http, NetTcp, Security) | 4.8.* | MIT |

## NuGet packages — test-only (not distributed with the product)

| Package | Version | License |
|---|---|---|
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| Moq | 4.20.72 | BSD-3-Clause |
| NUnit | 4.3.2 | MIT |
| NUnit.Analyzers | 4.9.2 | MIT |
| NUnit3TestAdapter | 5.0.0 | MIT |
| coverlet.collector | 6.0.4 | MIT |

## Attribution notices

This product includes software developed by The Legion of the Bouncy Castle
(https://www.bouncycastle.org).

Portions of this software use the Google APIs Client Library for .NET,
licensed under the Apache License, Version 2.0. You may obtain a copy of the
Apache-2.0 license at https://www.apache.org/licenses/LICENSE-2.0.

---

If you add or update a dependency, update this file and verify GPL v3
compatibility (CI runs a dependency license check; see `.github/workflows/ci.yml`).

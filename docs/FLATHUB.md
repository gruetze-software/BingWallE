# Flathub preparation

Internal project readiness notes, checked on 18 October 2026. This document is
AI-assisted upstream planning, not a submission message or a Flatpak manifest.
No Flathub submission, Flatpak build or sandbox test has been performed.

Update, 19 October 2026: the maintainer reports successful local Flatpak tests,
including menu integration, portal folder selection, downloads and reopening.
Both local linter checks completed without messages. Upstream release 1.3.3 is
tagged as `v1.3.3`; use its exact commit for final package validation. The original
preflight record below documents the earlier preparation, not Flathub acceptance.

For the publisher's German step-by-step guide, see
[FLATHUB-ANLEITUNG-DE.md](FLATHUB-ANLEITUNG-DE.md).

## Current upstream source

As of 23 October 2026, `main` targets version 1.5.0 with .NET 10 and Avalonia
12.1.4, native Wayland with X11 fallback, and the maintainer's hand-drawn icon.
See [Wayland details](WAYLAND.md) and [1.5.0 notes](RELEASE-1.5.0.md).
This source update does not publish or update a Flathub package.

## Historical preparation record (18–19 October 2026)

- Development version 1.3.3 targets .NET 10 LTS, including tests, recognition smoke
  checks and both GitHub workflows. Existing published packages are unchanged.
- Linux application ID: `de.gruetze_software.BingWallE`, derived from the publisher's
  `gruetze-software.de` domain. Domain control was stated by the publisher; HTTPS
  returned status 200 during preflight. The Flathub verification challenge remains open.
- AppStream source: `branding/de.gruetze_software.BingWallE.metainfo.xml`.
- Native Linux desktop source: `branding/de.gruetze_software.BingWallE.desktop`.
  Future DEB desktop/icon names match the ID. Snap keeps its own icon reference.
- English/German descriptions, MIT licence, developer, source/bug links and Ko-fi
  donation URL are present. Existing approved PNG artwork is available in Assets.
- Executable remains `bingwalle`; the package name and settings paths are unchanged.

The macOS bundle identity remains unchanged to avoid an unrelated migration.
New .NET 10 macOS packages require macOS 14 or later.

## Publisher and human maintainer checklist

1. Confirm the permanent app ID before submitting. It determines the domain used
   for verification. Ensure the domain works over HTTPS and links to BingWallE or
   Grütze-Software. Sign in to Flathub with the appropriate GitHub account.
2. Linux-rendered gallery views are stored in `branding/screenshots/1.3.3`, with
   provenance notes, and can be reproduced using `tools/StoreScreenshots`.
   These show the real app under the Linux headless platform with real Bing data,
   not Mint window decorations or a Flatpak sandbox. Validate their suitability
   against the current screenshot guidelines; a Mint desktop capture can replace
   them later. The user's original README screenshots remain intact.
3. Choose a maintained Flatpak runtime and .NET SDK extension. The upstream project
   now uses .NET 10, avoiding .NET 8's imminent support end on 10 November 2026.
4. A human must independently author the Flatpak manifest and dependency inputs
   and perform the submission. Follow the official .NET/offline-build guidance;
   do not fetch NuGet packages over the network during the sandboxed build.
5. Test an actual Flatpak build and install on Mint. Verify startup, folder-picker
   portals, download permissions, persistent settings and model cache, external
   browser links, image/folder opening and recognition shutdown. Source inspection
   shows an Avalonia folder picker but also external `xdg-open` calls: portal and
   sandbox behaviour has NOT yet been validated. Avoid broad host or home access
   merely to suppress failures; justify necessary access during human packaging.
6. Check desktop ID, icon names, AppStream, screenshots, release metadata and
   current Flatpak/Flathub lint requirements in the installed package.
7. Use tagged upstream release 1.3.3 and its exact commit as the release source,
   then rebuild and validate the package against that source.
8. Disclose known AI-generated/assisted code, docs, artwork and packaging with
   affected parts and approximate extent. This repository has substantial AI
   assistance, including the approved app icon and banner. Human review is needed
   to establish the complete scope; it must not be presented as a minor isolated
   change. Reviewer acceptance is discretionary.
9. The human maintainer must author and open the submission and all related
   messages, commit descriptions and review replies. This document is not a draft
   to paste into that process. An AI agent must not perform or review that submission.
10. After acceptance, complete the domain-verification challenge supplied by the
    Flathub dashboard. Do not invent or publish a token in advance. Verification
    matters for discoverability in Mint, where unverified Flatpaks can be hidden.

## Validation

The upstream metadata can be checked without submitting anything:

```bash
appstreamcli validate --no-net branding/de.gruetze_software.BingWallE.metainfo.xml
```

AppStream validation alone does not establish Flathub acceptance or Flatpak
compatibility. The prepared Linux views have pinned upstream image URLs; their
suitability, actual sandbox testing and human-created packaging remain open.

Completed locally on 18 October 2026:

- Release build under .NET 10: zero warnings and errors.
- Unit/integration tests: 54 passed, one optional native-model test skipped.
- Separate native inference and shutdown smoke check: three successful runs using
  the existing verified MobileNet model, including the telemetry opt-out.
- Existing headless collection/layout checks and information-dialog rendering pass
  with the matching Avalonia 11.3.20 headless dependency.
- Self-contained Linux x64 publish succeeds; the binary returns `BingWallE 1.3.3`
  under Ubuntu 24.04.4 WSL. This is a CLI startup check, not a Flatpak GUI test.
- AppStream validation and Linux/Snap shell syntax checks pass.

GitHub CI subsequently passed for the code and screenshots in commit `9ae38e8`:
Windows x64, Linux x64, macOS ARM64/x64 and the Snap build/startup checks.
See [platform build](https://github.com/gruetze-software/BingWallE/actions/runs/39147196793)
and [Snap build](https://github.com/gruetze-software/BingWallE/actions/runs/39147196717).
The changes are available in upstream draft [PR 1](https://github.com/gruetze-software/BingWallE/pull/1).
No release was published and no Flathub manifest was authored by the assistant.

## Official references

- [Requirements and AI policy](https://docs.flathub.org/docs/for-app-authors/requirements)
- [Submission process](https://docs.flathub.org/docs/for-app-authors/submission)
- [Domain verification](https://docs.flathub.org/docs/for-app-authors/verification)
- [MetaInfo guidelines](https://docs.flathub.org/docs/for-app-authors/metainfo-guidelines/)
- [.NET 10 SDK extension](https://github.com/flathub/org.freedesktop.Sdk.Extension.dotnet10)
- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

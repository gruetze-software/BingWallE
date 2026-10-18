# Linux Mint DEB test

The initial candidate is the existing Linux x64 DEB from successful GitHub Actions
run 39120812811, version 1.3.2. This is a test candidate, not a claim of completed
Linux Mint validation. It predates the new Ko-fi button in the information dialog.

## Target desktops

- Linux Mint 22.x Cinnamon, based on Ubuntu 24.04.
- Linux Mint 21.x Cinnamon, based on Ubuntu 22.04.
- MATE, Xfce, LMDE and ARM need separate validation.

References: [Mint releases](https://linuxmint.com/download_all.php),
[Mint 22 package base](https://linuxmint.com/rel_wilma_whatsnew.php).

## Install and start

Copy `BingWallE-1.3.2-linux-x64.deb` from the shared BingWallE folder to Downloads.
In a terminal opened in Downloads:

```bash
sudo apt install ./BingWallE-1.3.2-linux-x64.deb
/usr/bin/bingwalle
```

Use APT rather than `dpkg -i` so declared dependencies can be resolved. No .NET SDK,
separate .NET runtime or Snap setup is required. A graphical desktop is required.
Close an existing BingWallE process first. Snap and DEB settings may be separate.

## Manual checks

Preflight completed on Ubuntu 24.04.4 (WSL) on 18 October 2026: DEB control and
contents inspected (amd64, version 1.3.2, desktop entry, icon, AppStream metadata
and licences); extracted executable returned `BingWallE 1.3.2`; APT's simulated
installation resolved successfully without changing installed packages. This
does not verify Cinnamon integration, rendering or downloads on Mint itself.

1. Confirm the menu entry and icon, and start without a terminal.
2. Refresh Germany and USA; open a preview and check dimensions, copyright and tags.
3. Download Full HD and an available UHD image into Pictures. Check that the files
   open and that existing downloads are detected.
4. Test German/English, light/dark themes and reopening the application with its
   previous size or maximised state, including a small screen.
5. Test Show, Open Path and Ignore. Test Delete local on a disposable download only.
6. Optionally enable local recognition and verify model download and suggested tags.
7. Quit normally and reopen. Record the Mint version, desktop and any error output.

## Troubleshooting

Start from a terminal and keep its output if the application does not open:

```bash
/usr/bin/bingwalle 2>&1 | tee bingwalle-startup.log
```

The packaged .NET runtime still needs native system libraries, including ICU and
OpenSSL. If APT or startup reports a missing library, record the exact message and
Mint version rather than installing an arbitrary library from another distribution.
The 1.3.2 test candidate does not explicitly declare ICU and OpenSSL, although
both are normally installed on these desktops. The packaging script now declares
ICU and OpenSSL alternatives for future builds, including ICU 70 / OpenSSL 3 for
Mint 21 and ICU 74 / OpenSSL 3 t64 for Mint 22. The dependency list still needs
validation on actual Mint desktops.

Installing a newer DEB with `sudo apt install ./<new-package>.deb` upgrades the app.
An independently downloaded DEB does not register an APT update repository and is
not automatically discoverable in Mint's Software Manager.

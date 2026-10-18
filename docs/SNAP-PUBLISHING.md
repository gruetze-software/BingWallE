# Publishing BingWallE as a Snap

The package recipe targets amd64 desktop systems using the core24 base and GNOME
runtime integration, with strict confinement. Creating the test package needs no
publisher login. A Store account, registered name and completed desktop testing
are required before the first public beta.

## 1. Create your publisher account

Open https://snapcraft.io/account and create an Ubuntu One account. Verify your
email, configure account security, and set the public display name to
**Grütze-Software**. Choose a publisher username you want associated with releases.
Do not share passwords, login codes or authentication exports in chat or Git.

Register **bingwalle**, if available, at https://dashboard.snapcraft.io/register-snap/.
If asked, explain that you maintain the upstream open-source Bing wallpaper
collector at https://github.com/gruetze-software/BingWallE. A pending name review
must be accepted by Canonical before the first upload.
If unavailable, choose another name and update `name` in `snap/snapcraft.yaml`,
`Exec` in the snap desktop file and the workflow/test commands before publishing.
The display title remains **BingWallE**.

## 2. Obtain and locally test the package

Run the **Build and test Snap** GitHub Actions workflow and download the
`bingwalle-snap-amd64` artifact. It contains the `.snap` and startup diagnostics.
This workflow never uploads to the Store and requires no publisher credentials.
It checks application tests, installation with strict confinement, version output
and GUI startup under a virtual X display. It is not a substitute for interactive
file-picker and graphical-desktop testing.

To build yourself on Linux with .NET SDK 8, Snapcraft and its default isolated
build provider installed:

```sh
bash scripts/build-snap.sh
```

The preparation step publishes a self-contained .NET runtime into a dedicated
payload directory. Snapcraft packages that payload; uploading the manifest alone
to a remote build service is not supported. Destructive mode is used only on the
disposable Ubuntu 24.04 CI machine. Do not use that mode on your everyday system.

Install a downloaded test build on a desktop test system:

```sh
sudo snap install --dangerous ./bingwalle_1.3.2_amd64.snap
snap connections bingwalle
bingwalle
```

`--dangerous` permits installation of an unsigned local test file; it does not
disable the snap sandbox. Do not add `--devmode` or `--classic` for testing.

### Permissions and storage

- `network`: Bing archives, thumbnails, image downloads and the optional model download.
- `home`: non-hidden user-owned files, including the normal pictures destination.
- `removable-media`: optional access to mounted external drives. This normally
  needs an explicit connection by the user:
  `sudo snap connect bingwalle:removable-media`.
- GNOME extension: display, fonts, desktop portals and graphics integration.

Settings are stored in `$SNAP_USER_COMMON/config/bingwalle/settings.json` and the
recognition model in `$SNAP_USER_COMMON/models`. These locations survive revision
changes. Existing settings from an unpackaged installation are not automatically
imported from hidden host directories. When desktop integration cannot resolve a
host picture directory, the initial destination is `~/Pictures/BingImages` in the
real home; choose another folder in the application if needed.

Before beta release, test on Ubuntu with a normal desktop session:

1. Launch from the app menu and verify icon, DE/EN UI and light/dark modes.
2. Scan at least two regions; download Full HD and a supported UHD image.
3. Choose a destination using the file picker; reopen the app and download there again.
4. Test Show, Open Path, Ignore, confirmed Delete local and project web links.
5. Enable local recognition; complete the model download and inference, then quit.
6. Reopen and verify settings, model cache, ignored images and window restoration.
7. Test a mounted external drive if that is part of the intended workflow.

The file picker and opening folders/web links depend on the host desktop portal
integration. Treat failures in these actions as release blockers rather than
relaxing confinement. The local DEB/TAR.GZ remain available separately.

## 3. Fill in the Store listing

Use the copy and links in [SNAP-STORE-LISTING.md](SNAP-STORE-LISTING.md).
Use `BingWallE/Assets/bingwalle.png` as the Store icon (256 px, under 256 KB).
Both existing README screenshots are linked in the listing document; keep them
when editing the README. The public publisher name is managed by the Store
account, not by the package's `title` field.

## 4. Upload the first beta

On a trusted Linux workstation with Snapcraft installed:

```sh
snapcraft login
snapcraft upload --release=beta ./bingwalle_1.3.2_amd64.snap
```

Complete authentication yourself. Wait for the Store's review result. Test the
Store build with `sudo snap install bingwalle --beta`, then move the accepted
revision to stable only after the interactive checklist is complete:

```sh
snapcraft release bingwalle REVISION stable
```

Replace `REVISION` with the number shown by the successful upload. Store approval
and name availability are external requirements and are not guaranteed by CI.

## 5. Automate uploads later

After the initial beta and publisher setup work, a narrowly scoped Store token
can be placed in GitHub Actions secrets for beta uploads. Do not paste that token
into chat, commit it, or use an unrestricted account token. Stable promotion can
remain an explicit manual step. No Store upload secret or upload job is currently
configured.

## Official documentation

- https://ubuntu.com/docs/snapcraft/latest/how-to/publishing/register-a-snap/
- https://snapcraft.io/docs/releasing-your-app/
- https://ubuntu.com/docs/snapcraft/9/reference/extensions/gnome-extension/
- https://snapcraft.io/docs/reference/interfaces/home-interface/
- https://snapcraft.io/docs/reference/interfaces/removable-media-interface/

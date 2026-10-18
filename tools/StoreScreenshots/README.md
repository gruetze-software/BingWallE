# Linux app views

This upstream developer tool renders the real Avalonia controls with real Bing
captions and thumbnails. It is not a UI mockup or a Flatpak manifest generator.
Rendering is required to run in a Linux process. The headless platform omits OS
window decorations; this is not evidence of a Mint desktop or Flatpak sandbox test.

Use .NET 10 on the build machine, then run the published tool on Linux:

```sh
dotnet run --project tools/StoreScreenshots -- fetch artifacts/store-image-cache
dotnet publish tools/StoreScreenshots -c Release -r linux-x64 --self-contained true -o artifacts/store-renderer-linux
artifacts/store-renderer-linux/StoreScreenshots render artifacts/store-image-cache artifacts/store-linux-1.3.3
```

The fetch step records eight real en-US images; render can run offline with that
cache. Captions and photographer credits stay visible. The tool uses isolated
in-memory settings and does not change the user's actual application preferences.
Output includes light/dark gallery views, settings, about, and provenance notes.

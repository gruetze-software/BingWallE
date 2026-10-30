# English Linux store screenshots

This upstream developer tool renders the actual Avalonia controls on Linux, with
real Bing photographs, captions and photographer credits. It creates no UI mockups
or generated photographs. The headless platform omits OS window decorations;
these views are not evidence of a Mint desktop or Flatpak sandbox test.

The interface and Bing content region are English (`en`, `en-US`). The tool uses
isolated in-memory settings and a temporary collection; it never reads or changes
personal preferences or image folders.

With the .NET 10 SDK, fetch current photos and publish the Linux renderer:

```sh
dotnet run --project tools/StoreScreenshots -c Release -- fetch artifacts/store-image-cache-1.6.0
dotnet publish tools/StoreScreenshots -c Release -r linux-x64 --self-contained true -o artifacts/store-renderer-linux-1.6.0
```

Run rendering on Linux. Supply a directory containing the existing verified
`mobilenetv2-7.onnx` file from the application's local recognition cache; the example
assumes it has been copied to `artifacts/models`. Rendering uses cached data only:

```sh
artifacts/store-renderer-linux-1.6.0/StoreScreenshots render \
  artifacts/store-image-cache-1.6.0 branding/screenshots/1.6.0 artifacts/models
```

Output:

- `gallery-light.png` and `gallery-dark.png`: Discover, 1280 × 960.
- `collection-light.png`: My collection, 1280 × 960.
- `keywords-dark.png`: a genuinely resized preview showing the photo, credits,
  keyword chips, input and save controls, 1280 × 1280.
- `provenance.txt`: platform, content origin and capture details.

The keyword view uses real local inference. “My favorites” is entered using the
same Add command as a user would use; it is a sample personal keyword, not an AI
prediction. Photographer credits are retained. The temporary collection is removed
after rendering. The old `branding/screenshots/1.3.3` captures remain available.

# BingWallE 1.4.0

Released on 21 October 2026.

- Add optional Windows Spotlight images to the same gallery as Bing daily images.
  Enable the additional source in Settings; it is off by default. The selected
  download language and region apply to both sources independently of the UI language.
- Show each image's source on gallery cards and in the large preview. Save the
  source, image identity, image and information URLs, content region, description
  and photographer credits in downloaded JPEG metadata.
- Keep Spotlight images at the supplied original resolution. The HD / Full HD /
  UHD setting continues to control Bing downloads. Spotlight selections can vary
  between refreshes and use an unofficial Microsoft endpoint.
- Keep images from an available source when the other source fails, show the
  failure and allow retry. No Spotlight impression or tracking links are requested.
- Generate ASCII filenames using letters, digits and underscores. Simplify accents
  and remove or replace punctuation while retaining stable identifiers and collision
  protection. Recognize files saved under previous naming rules without renaming them.
- Fill missing creation or capture dates with the download time, explicitly recording
  fallback origins and the download timestamp in XMP. Preserve existing dates, credits
  and camera metadata; JPEG image data is not recompressed.
- Fix Windows Open Path for file names containing commas by quoting the full path
  passed to Explorer.
- Widen Settings and About and reserve space for their scrollbars. Center button
  captions throughout the application; retain a coffee icon only on the Ko-fi button.
- Keep the full version number visible, including the final zero in 1.4.0.

Windows functionality was manually tested by the maintainer. Automated coverage
includes both sources, language/region handling, download metadata, naming rules,
legacy-file recognition and button layout. Desktop packages are built and tested
for Windows x64, Linux x64, macOS Apple Silicon and macOS Intel.

GitHub releases do not automatically publish to the Snap Store or Flathub.
The separate Snap workflow builds and smoke-tests a confined amd64 package.

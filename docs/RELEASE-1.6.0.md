# BingWallE 1.6.0

Released on 30 October 2026.

## My collection

- Switch between Discover and My collection without repeating the online search.
  Discover retains its loaded images and selection.
- Browse older and current downloads in the selected folder. Read embedded titles,
  descriptions, sources and keywords; use filenames when metadata is missing.
- Open local previews offline. Refresh rereads the local folder, and Open folder
  continues to open the file manager. Local file actions use the exact displayed file,
  independent of the selected download resolution.
- Support JPEG, PNG and WebP browsing, skip unreadable images and allow cancellation.

## Editable and personal keywords

- Remove unwanted keywords or incorrect AI suggestions with ×, add personal terms,
  and run local recognition again on demand.
- Keep personal keywords when recognizing again. Save excluded terms so unwanted
  suggestions stay excluded after reopening, rerunning recognition or redownloading.
- Save reviewed keywords explicitly in JPEG EXIF/XMP. Editing and recognition alone
  leave the file unchanged; edits to images not yet downloaded are applied on download.
- Preserve compressed image data, camera fields, dates, copyright and unrelated
  metadata. Keyword writing into existing PNG/WebP files is not supported yet.
- Read existing keywords from files. Extract text keywords from titles and descriptions
  together, including wolves, coyotes and broader animal categories.

## Window and menu improvements

- Remember the normal size and maximized state separately for the main and preview
  windows. Preserve normal bounds when closing maximized, minimized or fullscreen.
- Correct Windows resize/state notification ordering so maximized dimensions do not
  overwrite the saved normal size.
- Render gallery context menus inside the current window, with sufficient space for
  every command. Highlight ignored labels subtly in light and dark themes.

The application uses .NET 10 and Avalonia 12.1.4, with native Wayland and automatic
X11 fallback on Linux. The minimum macOS version remains 14. Packages are provided
for Windows x64, Linux x64, and macOS ARM64/x64. GitHub releases do not automatically
update the Snap Store or Flathub; those stores are published separately.

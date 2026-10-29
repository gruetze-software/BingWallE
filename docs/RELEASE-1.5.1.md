# BingWallE 1.5.1

Released on 29 October 2026.

- On first start, use the system UI language automatically: German for German
  locales and English otherwise. Existing supported language preferences remain
  unchanged; older settings without a language use the same system default.
  The download region remains independent of the interface language.
- F11 toggles fullscreen in the main window and the image preview. Exiting restores
  the previous normal or maximized state, including when fullscreen was entered
  with the native titlebar button. Closing the main window while fullscreen retains
  its previous maximized preference rather than storing fullscreen as a startup mode.
- Escape closes an open preview before exiting the main window's fullscreen mode.
  In the preview window, Escape exits its fullscreen mode first; a subsequent Escape
  closes the preview. Escape in the normal main window does not quit the application.
- The image preview is now a separate owned window with resize and maximize support.
  Its image scales proportionally with the available area without cropping.
  Description and metadata scroll separately, keeping the image, close button and
  save status visible. Closing cancels pending image loading and local recognition.

- Make language-dependent regression tests explicit and verify partial Spotlight
  failures in both supported interface languages on non-German build machines.

Existing supported language choices remain unchanged, including German saved by
an earlier version. Change that choice once in Settings if desired. The image
region remains independent of the interface language.

The application continues to use .NET 10 and Avalonia 12.1.4. The minimum macOS
version remains 14. Packages are available for Windows x64, Linux x64 and macOS
ARM64/x64. GitHub releases do not automatically update the Snap Store or Flathub.

# Changes collected after 1.5.0

These changes are not part of the published 1.5.0 packages. No new version tag
or Store upload has been created.

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

Test from Visual Studio or an unpublished build. Check native window behavior on
Ubuntu Wayland as well as Windows; headless UI tests do not replace that desktop test.

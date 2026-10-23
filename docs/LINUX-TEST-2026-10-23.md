# Linux VM test review, 2026-10-23

Branch: `codex/avalonia12-wayland`. Review of four user-provided logs.
The logs confirm backend initialization, not completion of every interactive test.

| Log | Result |
| --- | --- |
| `mint_x11test.log` | Initialized `Avalonia.X11.X11Window`. No further messages. |
| `mint_waylandtest.log` | Forced Wayland could not connect to a display. This does not test automatic fallback and does not establish a Wayland rendering defect. Use X11 or an accessible Wayland session. |
| `ubuntu_test.log` | Initialized `Avalonia.Wayland.WindowImpl`: native Wayland confirmed. Mesa/EGL/Zink driver warnings precede initialization; they did not prevent that startup. The log alone cannot establish whether rendering remained correct. |
| `ubuntu_test_x11.log` | Initialized `Avalonia.X11.X11Window`. Later snap mount-namespace warnings and a GTK module message are present. They are not an application exception; the log does not identify which launched helper produced them. |

## Image properties show `lang="x-default"`

The screenshot shows the language qualifier before title, description and copyright.
BingWallE writes these Dublin Core XMP fields as `rdf:Alt` language alternatives,
with `xml:lang="x-default"` on the `rdf:li` element. The qualifier is an XML
attribute and is not included in the title, description or copyright text.

Reproduced independently using Ubuntu 24.04 GExiv2 0.14.2 and Exiv2 0.27.6:
`get_tag_interpreted_string("Xmp.dc.title")` produces
`lang="x-default" Küste & Vögel` for a generated BingWallE JPEG, whereas the
XML text value is `Küste & Vögel`. Description and rights behave likewise.
Nautilus uses this interpreted-string API when displaying these fields.

This is a reader/display compatibility issue. Removing language alternatives or
writing these properties as plain strings would sacrifice standard XMP types.
Keep the valid metadata. A regression test checks all three fields, Unicode,
separation of the language attribute from the text and unchanged JPEG scan data.
Existing downloads do not need to be rewritten for this display issue.

## Remaining checks

- Confirm interactive rendering, input, dialogs, scaling and context menus in the
  tested Ubuntu Wayland session; backend initialization alone does not prove them.
- Test automatic startup in the Mint session, without forcing Wayland, to confirm
  usable X11 fallback in that environment.
- Complete the separate real Flatpak sandbox and macOS interactive tests before release.

## References

- [Nautilus image-properties reader](https://github.com/GNOME/nautilus/blob/main/extensions/image-properties/nautilus-image-properties-model.c)
- [GExiv2 interpreted XMP values](https://github.com/GNOME/gexiv2/blob/main/gexiv2/gexiv2-metadata-xmp.cpp)
- [Exiv2 LangAlt documentation](https://exiv2.org/manpage.html#langalt)

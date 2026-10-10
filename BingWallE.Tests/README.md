# Tests

Im Repository-Stamm ausführen:

```sh
dotnet test BingWallE.sln -c Release
```

`RegressionTests` sichert Abbruch, sichere Downloads und Dateinamen ab.
`ServiceTests` prüft das Bing-Protokoll, Einstellungen und Ordnerauswahl.
HTTP-Antworten werden simuliert. Dateitests verwenden eigene temporäre Ordner,
die nach dem Test gelöscht werden. Ein grafischer Desktop wird nicht benötigt.

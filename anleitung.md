# Jellythek

<div align="center">
    <p>
        <img alt="Logo" src="https://github.com/cibkp89/jellyfin-plugin-jellythek/blob/main/images/Jellythek.png?raw=true" height="350"/></br>

[![Latest Release](https://img.shields.io/github/v/release/cibkp89/jellyfin-plugin-jellythek)](https://github.com/cibkp89/jellyfin-plugin-jellythek/releases)
[![License](https://img.shields.io/github/license/cibkp89/jellyfin-plugin-jellythek)](https://github.com/cibkp89/jellyfin-plugin-jellythek/blob/main/LICENSE)
[![Open Issues](https://img.shields.io/github/issues/cibkp89/jellyfin-plugin-jellythek)](https://github.com/cibkp89/jellyfin-plugin-jellythek/issues)
<a href="https://jellyfin.org/"><img alt="Jellyfin Version" src="https://img.shields.io/badge/Jellyfin-10.11-blue.svg"/></img>

</div>

Ein Jellyfin-Plugin, das RSS-Feeds von [MediathekViewWeb](https://mediathekviewweb.de/) abruft und als .strm-Dateien für Jellyfin bereitstellt.

  <p>
    <a href="#features">Features</a> •
    <a href="#installation">Installation</a> •
    <a href="#konfiguration">Konfiguration</a> •
    <a href="#Systemanforderungen und Abhängigkeiten">Systemanforderungen</a> •   
    <a href="#Support">Support</a> •         
    <a href="#license">License</a> •
    <a href="#acknowledgments">Acknowledgments</a>
  </p>

## Features
  ✨ Automatische Synchronisation von MediathekView-Inhalten <br>
  📺 Serien & Filme werden unterstützt <br>
  🎯 Flexible Filter mit MediathekView-Syntax (!sender, >dauer, etc.) <br>
  🔄 Clean Folder - Automatisches Aufräumen alter Episoden (nur bei Serien) <br>
  🌍 Proxy-Unterstützung für DE, AT und CH Inhalte <br>
  📝 Metadaten werden automatisch als .nfo-Dateien erstellt <br>
  🎬 Staffel/Episode Extraktion für bessere Organisation <br>
  🚫 Ausschluss-Filter für unerwünschte Inhalte <br>

## Installation

### Via Plugin-Repository (empfohlen)
1. Öffne Jellyfin **Dashboard → Plugins → Repositories**
2. Füge folgendes Repository hinzu:
   - **Name:** Jellythek Repository
   - **URL:** https://raw.githubusercontent.com/cibkp89/jellyfin-plugin-jellythek/main/manifest.json
3. Gehe zu Plugins → Katalog **Plugins → Katalog**
4. Suche nach "Jellythek" und installiere es
5. Starte Jellyfin neu

### Manuelle Installation
1. Lade die neueste Jellyfin.Plugin.Jellythek-vX.X.X.zip von [Releases](../../releases) herunter
2. Entpacke das ZIP-Archiv
3. Kopiere alle DLL-Dateien nach /config/plugins/Jellythek_vX.X.X
   - Jellyfin.Plugin.Jellythek.dll
   - System.ServiceModel.Syndication.dll
4. Starte Jellyfin neu     

## Konfiguration
### Einstellungen
Nach der Installation findest du die Konfiguration unter: Dashboard → Plugins → Jellythek

Tab 1: Allgemein
- Synchronisation aktiviert: Master-Schalter für die Sync-Funktion
- Speicherpfad: Wo die .strm-Dateien abgelegt werden (Standard: /config/data/jellythek)
- URL Präfix DE/AT/CH: Optional - URLs für Proxy-Server (z.b Flask)

Tab 2: Neuer Feed
Erstelle einen neuen Feed mit folgenden Optionen:
- Name: Anzeigename des Feeds (z.B. "Tatort")
- Filter: MediathekView-Syntax (z.B. !ard tatort >60)<br>
- Typ: Serie oder Film
- Staffel/Episode extrahieren: Erkennt S01E05-Muster im Titel
- Max. Einträge: Begrenzt die Anzahl der Einträge (z.B. nur die letzten 5)
- Ordner vor Sync leeren: Löscht alte Einträge (nur bei Serien!)
- Proxy: Wähle DE, AT oder CH Proxy
- Ausschluss-Begriffe: Filter für unerwünschte Inhalte (z.B. "Audiodeskription")

Tab 3: Gespeicherte Feeds
Übersicht aller konfigurierten Feeds mit Lösch-Funktion.

### Geplante Aufgaben
Die Synchronisation kann über Dashboard → Geplante Aufgaben → Jellythek Synchronisation zeitgesteuert werden.
Empfohlener Zeitplan: Täglich um 3:00 Uhr

### Bibliothek hinzufügen
Nach der Konfiguration erstelle eine neue Bibliothek in Jellyfin:

1. Dashboard → Bibliotheken → Bibliothek hinzufügen
2. Inhaltstyp: Filme oder Serien (je nach Feed)
3. Ordner: /config/data/jellythek/Filme oder /config/data/jellythek/Serien
4. Metadaten-Downloader: Aktiviere gewünschte Scraper (TMDB, IMDB, etc.)

### Wichtige Hinweise
⚠️ Clean Folder bei Filmen: Diese Option ist nur für Serien sinnvoll und wird bei Film-Feeds automatisch ignoriert, da alle Filme im gemeinsamen /Filme/ Ordner liegen.<br>
💡 Metadaten-Erkennung: Filme werden ohne Datumspräfix gespeichert, damit Jellyfin die Metadaten automatisch abrufen kann.<br>
🔄 Duplikate: Filme mit gleichem Titel werden automatisch überschrieben (neueste Version bleibt).

## Systemanforderungen und Abhängigkeiten
- Jellyfin 10.11.0 oder höher
- .NET 9.0

Das Plugin benötigt System.ServiceModel.Syndication.dll (wird im Release-ZIP mitgeliefert) 

### Build from Source
`git clone https://github.com/cibkp89/jellyfin-plugin-jellythek.git` <br>
`cd jellyfin-plugin-jellythek/Jellyfin.Plugin.Jellythek` <br>
`dotnet build`

## Support
Bei Problemen oder Fragen erstelle bitte ein Issue auf GitHub.

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

Copyright (C) 2025 cibkp89

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

## Acknowledgments

- **[mediathekview.de](https://mediathekview.de/)**: Das Programm MediathekView durchsucht die Online-Mediatheken verschiedener Sender und listet die gefundenen Sendungen auf. Die Liste kann mit verschiedenen Filtern nach Beiträgen durchsucht werden. Mit einem Programm deiner Wahl können die Filme angesehen und aufgezeichnet werden. Es lassen sich Abos anlegen und neue Beiträge automatisch herunterladen.
- **[mediathekviewweb.de](https://mediathekviewweb.de/)**: Web Client von MediathekView
- **[Jellyfin](https://jellyfin.org)**: Open-source media server platform
- **[Claude.ai](https://claude.ai/)**: Claude ist ein KI-Chatbot. Er ist fähig, eine Vielzahl von Aufgaben wie Textzusammenfassung, Code-Schreiben, kreatives Schreiben und mehr auszuführen

---

<div align="center">
  Made with ❤️ for the Jellyfin community
</div>

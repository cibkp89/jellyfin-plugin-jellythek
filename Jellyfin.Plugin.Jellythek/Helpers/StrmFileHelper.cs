using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellythek.Helpers;

/// <summary>
/// Hilfklasse für die Erstellung von .strm-Dateien.
/// </summary>
public class StrmFileHelper
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrmFileHelper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public StrmFileHelper(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Erstellt eine .strm-Datei mit der Video-URL als Inhalt.
    /// </summary>
    /// <param name="title">Der Titel (wird zum Dateinamen).</param>
    /// <param name="videoUrl">Die Video-URL (Inhalt der .strm-Datei).</param>
    /// <param name="outputDir">Das Zielverzeichnis.</param>
    public void CreateStrmFile(string title, string videoUrl, string outputDir)
    {
        try
        {
            // 1. Dateinamen bereinigen: Ungültige Zeichen für Dateinamen entfernen
            var safeTitle = SanitizeFileName(title);

            // 2. Den vollständigen Pfad zur .strm-Datei erstellen
            var filepath = Path.Combine(outputDir, $"{safeTitle}.strm");

            // 3. Sicherstellen, dass das Verzeichnis existiert
            Directory.CreateDirectory(outputDir);

            // 4. Datei schreiben: Inhalt ist nur die Video-URL
            File.WriteAllText(filepath, videoUrl);

            _logger.LogInformation("[ERFOLG] Erstellt: {FileName} in {Directory}", Path.GetFileName(filepath), outputDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FEHLER] Konnte .strm-Datei nicht erstellen: {Title}", title);
        }
    }

    /// <summary>
    /// Erstellt eine .nfo-Datei mit Metadaten für Jellyfin.
    /// </summary>
    /// <param name="title">Der Titel.</param>
    /// <param name="description">Die Beschreibung.</param>
    /// <param name="publishDate">Das Veröffentlichungsdatum.</param>
    /// <param name="outputDir">Das Zielverzeichnis.</param>
    public void CreateNfoFile(string title, string? description, DateTime? publishDate, string outputDir)
    {
        try
        {
            var safeTitle = SanitizeFileName(title);
            var filepath = Path.Combine(outputDir, $"{safeTitle}.nfo");

            Directory.CreateDirectory(outputDir);

            // Einfache NFO-Struktur für Jellyfin
            var nfoContent = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n";
            nfoContent += "<movie>\n";
            nfoContent += $"  <title>{System.Security.SecurityElement.Escape(title)}</title>\n";
            
            if (!string.IsNullOrWhiteSpace(description))
            {
                nfoContent += $"  <plot>{System.Security.SecurityElement.Escape(description)}</plot>\n";
            }

            if (publishDate.HasValue)
            {
                nfoContent += $"  <aired>{publishDate.Value:yyyy-MM-dd}</aired>\n";
                nfoContent += $"  <premiered>{publishDate.Value:yyyy-MM-dd}</premiered>\n";
            }

            nfoContent += "</movie>";

            File.WriteAllText(filepath, nfoContent);

            _logger.LogDebug("[NFO] Erstellt: {FileName}", Path.GetFileName(filepath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FEHLER] Konnte .nfo-Datei nicht erstellen: {Title}", title);
        }
    }

    /// <summary>
    /// Bereinigt einen Dateinamen von ungültigen Zeichen.
    /// </summary>
    /// <param name="fileName">Der zu bereinigende Dateiname.</param>
    /// <returns>Der bereinigte Dateiname.</returns>
    public static string SanitizeFileName(string fileName)
    {
        // Entfernt ungültige Zeichen für Dateinamen
        var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var regex = new Regex($"[{Regex.Escape(invalid)}]");
        return regex.Replace(fileName, string.Empty).Trim();
    }

    /// <summary>
    /// Löscht alle .strm und .nfo Dateien in einem Verzeichnis (rekursiv).
    /// </summary>
    /// <param name="directory">Das zu bereinigende Verzeichnis.</param>
    /// <param name="recursive">Sollen Unterordner auch bereinigt werden?</param>
    public void CleanDirectory(string directory, bool recursive = true)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogWarning("[CLEAN] Verzeichnis existiert nicht: {Directory}", directory);
            return;
        }

        try
        {
            _logger.LogInformation("[CLEAN] Starte Bereinigung von: {Directory} (rekursiv: {Recursive})", directory, recursive);

            // Zähler für gelöschte Dateien
            int deletedStrm = 0;
            int deletedNfo = 0;
            int deletedDirs = 0;

            // Lösche .strm Dateien im aktuellen Verzeichnis
            var strmFiles = Directory.GetFiles(directory, "*.strm");
            _logger.LogInformation("[CLEAN] Gefunden: {Count} .strm Dateien in {Directory}", strmFiles.Length, directory);
            
            foreach (var file in strmFiles)
            {
                try
                {
                    File.Delete(file);
                    deletedStrm++;
                    _logger.LogDebug("[CLEAN] Gelöscht (STRM): {File}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CLEAN] FEHLER beim Löschen von {File}", file);
                }
            }

            // Lösche .nfo Dateien im aktuellen Verzeichnis
            var nfoFiles = Directory.GetFiles(directory, "*.nfo");
            _logger.LogInformation("[CLEAN] Gefunden: {Count} .nfo Dateien in {Directory}", nfoFiles.Length, directory);
            
            foreach (var file in nfoFiles)
            {
                try
                {
                    File.Delete(file);
                    deletedNfo++;
                    _logger.LogDebug("[CLEAN] Gelöscht (NFO): {File}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CLEAN] FEHLER beim Löschen von {File}", file);
                }
            }

            // Rekursiv Unterordner bereinigen
            if (recursive)
            {
                var subDirs = Directory.GetDirectories(directory);
                _logger.LogInformation("[CLEAN] Gefunden: {Count} Unterordner in {Directory}", subDirs.Length, directory);
                
                foreach (var subDir in subDirs)
                {
                    _logger.LogDebug("[CLEAN] Bereinige Unterordner: {SubDir}", subDir);
                    CleanDirectory(subDir, recursive);
                    
                    // Leere Unterordner löschen
                    try
                    {
                        if (Directory.GetFiles(subDir).Length == 0 && Directory.GetDirectories(subDir).Length == 0)
                        {
                            Directory.Delete(subDir);
                            deletedDirs++;
                            _logger.LogInformation("[CLEAN] Leerer Ordner gelöscht: {Dir}", subDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[CLEAN] FEHLER beim Löschen von Ordner {Dir}", subDir);
                    }
                }
            }

            _logger.LogInformation(
                "[CLEAN] Bereinigung abgeschlossen für {Directory}: {StrmCount} .strm, {NfoCount} .nfo, {DirCount} Ordner gelöscht",
                directory, deletedStrm, deletedNfo, deletedDirs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FEHLER] Konnte Ordner {Directory} nicht bereinigen", directory);
        }
    }
}
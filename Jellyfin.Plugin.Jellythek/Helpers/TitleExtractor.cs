using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellythek.Helpers;

/// <summary>
/// Extrahiert Staffel- und Episoden-Nummern aus Titeln.
/// Entspricht der title_extractor.py aus dem Python-Script.
/// </summary>
public static class TitleExtractor
{
    /// <summary>
    /// Versucht, Staffel (S) und Episode (E) Nummern aus dem Text zu extrahieren
    /// anhand gängiger Muster (SXXEXX, Staffel X Folge Y, Staffel X (Y/Z), SXX/EXX).
    /// </summary>
    /// <param name="text">Der zu analysierende Text.</param>
    /// <param name="season">Extrahierte Staffelnummer oder null.</param>
    /// <param name="episode">Extrahierte Episodennummer oder null.</param>
    public static void ExtractSeasonEpisode(string text, out int? season, out int? episode)
    {
        season = null;
        episode = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Muster 1: SXXEXX (z.B. S01E05)
        var matchSxe = Regex.Match(text, @"[sS](\d{1,2})[eE](\d{1,3})", RegexOptions.None);
        if (matchSxe.Success)
        {
            if (int.TryParse(matchSxe.Groups[1].Value, out int s) && 
                int.TryParse(matchSxe.Groups[2].Value, out int e))
            {
                season = s;
                episode = e;
                return;
            }
        }

        // Muster 5 (NEU): SXX/EXX (z.B. S01/E02)
        var matchSSlashE = Regex.Match(text, @"[sS](\d{1,2})\s*[/]\s*[eE](\d{1,3})", RegexOptions.None);
        if (matchSSlashE.Success)
        {
            if (int.TryParse(matchSSlashE.Groups[1].Value, out int s) && 
                int.TryParse(matchSSlashE.Groups[2].Value, out int e))
            {
                season = s;
                episode = e;
                return;
            }
        }

        // Muster 2: "Staffel X Folge Y" oder "Staffel X, Episode Y"
        var matchSf = Regex.Match(
            text, 
            @"Staffel\s+(\d{1,2}).*?(?:Folge|Episode)\s+(\d{1,3})", 
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (matchSf.Success)
        {
            if (int.TryParse(matchSf.Groups[1].Value, out int s) && 
                int.TryParse(matchSf.Groups[2].Value, out int e))
            {
                season = s;
                episode = e;
                return;
            }
        }

        // Muster 3: "Folge Y aus Staffel X" (umgekehrte Reihenfolge)
        var matchFs = Regex.Match(
            text, 
            @"(?:Folge|Episode)\s+(\d{1,3}).*?Staffel\s+(\d{1,2})", 
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (matchFs.Success)
        {
            if (int.TryParse(matchFs.Groups[2].Value, out int s) && 
                int.TryParse(matchFs.Groups[1].Value, out int e))
            {
                season = s;
                episode = e;
                return;
            }
        }

        // Muster 4: "Staffel X (Y/Z)" oder "Season X (Y/Z)" -> Season X, Episode Y
        var matchSParenE = Regex.Match(
            text, 
            @"(?:Staffel|Season)\s*(\d{1,2})\s*[-:]*\s*\((\d{1,3})\s*/\s*\d{1,3}\s*\)", 
            RegexOptions.IgnoreCase);
        if (matchSParenE.Success)
        {
            if (int.TryParse(matchSParenE.Groups[1].Value, out int s) && 
                int.TryParse(matchSParenE.Groups[2].Value, out int e))
            {
                season = s;
                episode = e;
                return;
            }
        }
    }

    /// <summary>
    /// Extrahiert Staffel/Episode (falls aktiviert), bereinigt den Titel
    /// und erstellt den endgültigen Titel für die .strm-Datei.
    /// </summary>
    /// <param name="originalTitle">Der ursprüngliche Titel aus dem RSS-Feed.</param>
    /// <param name="extractSe">Soll Staffel/Episode extrahiert werden?</param>
    /// <param name="datePrefix">Optionales Datums-Präfix (z.B. "2024-11-15").</param>
    /// <param name="logger">Logger für Debug-Ausgaben.</param>
    /// <returns>Tupel aus (finalTitle, currentSeason).</returns>
    public static (string FinalTitle, int CurrentSeason) CleanAndFormatTitle(
        string originalTitle, 
        bool extractSe, 
        string? datePrefix,
        ILogger? logger = null)
    {
        string finalTitle = originalTitle;
        int currentSeason = 1;

        if (extractSe)
        {
            logger?.LogDebug("[DEBUG] Extraktions-Text: '{Title}'", originalTitle);
            
            ExtractSeasonEpisode(originalTitle, out int? season, out int? episode);

            if (season.HasValue && episode.HasValue)
            {
                currentSeason = season.Value;
                string seTag = $"S{season.Value:D2}E{episode.Value:D2}";
                string baseTitle = originalTitle;

                // --- BEREINIGUNG DES TITELS: ALLE S/E-MUSTER ENTFERNEN ---

                // Muster 1 & 5: SXXEXX und SXX/EXX (Wir entfernen alle S/E-Tags)
                baseTitle = Regex.Replace(baseTitle, @"[sS]\d{1,2}[eE]\d{1,3}", string.Empty);
                baseTitle = Regex.Replace(baseTitle, @"[sS]\d{1,2}\s*[/]\s*[eE]\d{1,3}", string.Empty);

                // Muster 2/3: Staffel X Folge Y, Folge Y aus Staffel X
                baseTitle = Regex.Replace(
                    baseTitle,
                    @"(?:Staffel|Episode|Folge)\s*\d{1,3}(?:\s*.*?\s*(?:aus|von|in)\s*(?:Staffel|Season)\s*\d{1,2})?",
                    string.Empty,
                    RegexOptions.IgnoreCase);

                // Muster 4: Entfernt: "Staffel 2 (4/6)"
                baseTitle = Regex.Replace(
                    baseTitle, 
                    @"(?:Staffel|Season)\s+\d{1,2}\s*[-:]*\s*\(\s*\d{1,3}\s*/\s*\d{1,3}\s*\)", 
                    string.Empty, 
                    RegexOptions.IgnoreCase);

                // Entfernt Titel-Präfixe wie 'The Newsreader - ' selbst, falls nur Leerzeichen übrig sind
                baseTitle = Regex.Replace(baseTitle, @"^\s*[-:]*\s*", string.Empty);

                // Entfernt nun alle übrigen Klammerinhalte
                baseTitle = Regex.Replace(baseTitle, @"\s*\([^)]*\)\s*", " ");
                baseTitle = baseTitle.Trim(' ', '-', ':');

                // --- ENDE BEREINIGUNG ---

                logger?.LogInformation(
                    "[EXTRAKTION] Erfolg: {SeTag} aus '{OriginalTitle}' -> Bereinigt: '{CleanTitle}'",
                    seTag, originalTitle, baseTitle);

                // Kombiniert Datum, S/E und bereinigten Titel
                if (!string.IsNullOrWhiteSpace(datePrefix))
                {
                    finalTitle = $"{datePrefix} - {seTag} - {baseTitle}";
                }
                else
                {
                    finalTitle = $"{seTag} - {baseTitle}";
                }
            }
            else
            {
                logger?.LogWarning(
                    "[EXTRAKTION] KEIN S/E-Muster gefunden für Titel: '{Title}'", 
                    originalTitle);
                logger?.LogInformation("[INFO] Verwende Standard: Datum und Staffel 1.");

                // Wenn keine Extraktion, füge trotzdem das Datum hinzu, falls vorhanden
                if (!string.IsNullOrWhiteSpace(datePrefix))
                {
                    finalTitle = $"{datePrefix} - {originalTitle}";
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(datePrefix))
        {
            // Wenn extract_se=False, wird nur das Datum hinzugefügt (falls vorhanden)
            finalTitle = $"{datePrefix} - {originalTitle}";
        }

        return (finalTitle.Trim(), currentSeason);
    }
}
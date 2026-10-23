using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellythek.Configuration;
using Jellyfin.Plugin.Jellythek.Helpers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellythek.ScheduledTasks;

/// <summary>
/// Scheduled Task zur Synchronisation der MediathekView RSS-Feeds.
/// Version mit RssFeedParser (ohne StrmFileHelper).
/// </summary>
public class MediathekSyncTask : IScheduledTask
{
    private readonly ILogger<MediathekSyncTask> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediathekSyncTask"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="libraryManager">Library manager for triggering scans.</param>
    public MediathekSyncTask(
        ILogger<MediathekSyncTask> logger,
        IHttpClientFactory httpClientFactory,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public string Name => "Jellythek Synchronisation";

    /// <inheritdoc />
    public string Key => "JellythekSync";

    /// <inheritdoc />
    public string Description => "Synchronisiert RSS-Feeds von MediathekViewWeb";

    /// <inheritdoc />
    public string Category => "Jellythek";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || !config.SyncEnabled)
        {
            _logger.LogInformation("MediathekView Synchronisation ist deaktiviert.");
            return;
        }

        _logger.LogInformation("--- Starte MediathekView Synchronisation ---");
        _logger.LogInformation("Zu verarbeitende Feeds: {Count}", config.Feeds.Count);

        var baseOutputDir = GetMediathekDataPath();
        _logger.LogInformation("Basis-Ausgabeverzeichnis: {Path}", baseOutputDir);

        // Sicherstellen, dass das Basis-Verzeichnis existiert
        Directory.CreateDirectory(baseOutputDir);

        var rssParser = new RssFeedParser(_logger, _httpClientFactory);
        var strmHelper = new StrmFileHelper(_logger);

        int processedFeeds = 0;
        foreach (var feedConfig in config.Feeds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessFeedAsync(feedConfig, baseOutputDir, rssParser, strmHelper, config.ProxyUrlDe, config.ProxyUrlAt, config.ProxyUrlCh, cancellationToken)
                .ConfigureAwait(false);

            processedFeeds++;
            progress.Report((double)processedFeeds / config.Feeds.Count * 100);
        }

        _logger.LogInformation("--- MediathekView Synchronisation abgeschlossen ---");
    }

    /// <summary>
    /// Verarbeitet einen einzelnen RSS-Feed.
    /// </summary>
    private async Task ProcessFeedAsync(
        FeedConfig feedConfig,
        string baseOutputDir,
        RssFeedParser rssParser,
        StrmFileHelper strmHelper,
        string proxyUrlDe,
        string proxyUrlAt,
        string proxyUrlCh,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("--- Verarbeite Feed: {FeedName} ---", feedConfig.Name);
        _logger.LogInformation("  Filter: '{Filter}' | Typ: {Type} | Staffel-Extraktion: {Staffel}",
            feedConfig.Filter, feedConfig.Type, feedConfig.Staffel);

        if (feedConfig.Exclude.Any())
        {
            _logger.LogInformation("  Ausschluss-Liste: {Exclude}", string.Join(", ", feedConfig.Exclude));
        }

        // Bestimme Zielverzeichnis basierend auf Typ
        var feedBaseDir = GetFeedDirectory(baseOutputDir, feedConfig);
        _logger.LogInformation("  Zielverzeichnis: {Path}", feedBaseDir);

        // Ordner bereinigen falls gewünscht - NUR FÜR SERIEN
        // Bei Filmen wird CleanFolder ignoriert, da alle Filme im gemeinsamen /Filme/ Ordner liegen
        if (feedConfig.CleanFolder && feedConfig.Type == "serie")
        {
            _logger.LogInformation("  Starte CleanDirectory für: {Path}", feedBaseDir);
            strmHelper.CleanDirectory(feedBaseDir);
            _logger.LogInformation("  CleanDirectory abgeschlossen");
        }
        else if (feedConfig.CleanFolder && feedConfig.Type == "film")
        {
            _logger.LogWarning(
                "[INFO] CleanFolder ist für Film-Feed '{FeedName}' aktiviert, wird aber ignoriert. " +
                "CleanFolder funktioniert nur bei Serien, da Filme im gemeinsamen /Filme/ Ordner liegen.",
                feedConfig.Name);
        }

        // RSS-Feed abrufen
        var feedUrl = RssFeedParser.GetFeedUrl(feedConfig.Filter);
        var entries = await rssParser.ParseFeedAsync(feedUrl, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("  RSS-Feed hat {Count} Einträge geliefert", entries.Count);

        // Maximal zu verarbeitende Einträge begrenzen
        if (feedConfig.MaxEntries.HasValue && feedConfig.MaxEntries.Value > 0)
        {
            entries = entries.Take(feedConfig.MaxEntries.Value).ToList();
            _logger.LogInformation("  Nach MaxEntries-Filter: {Count} Einträge werden verarbeitet", entries.Count);
        }

        // Einträge verarbeiten
        int processedCount = 0;
        foreach (var entry in entries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            ProcessEntry(entry, feedConfig, feedBaseDir, strmHelper, proxyUrlDe, proxyUrlAt, proxyUrlCh);
            processedCount++;
        }

        _logger.LogInformation("  Verarbeitung abgeschlossen: {Count} Dateien erstellt", processedCount);
    }

    /// <summary>
    /// Verarbeitet einen einzelnen Feed-Eintrag.
    /// </summary>
    private void ProcessEntry(
        FeedEntry entry,
        FeedConfig feedConfig,
        string feedBaseDir,
        StrmFileHelper strmHelper,
        string proxyUrlDe,
        string proxyUrlAt,
        string proxyUrlCh)
    {
        // Ausschluss-Logik prüfen
        if (ShouldExcludeEntry(entry.Title, feedConfig.Exclude))
        {
            _logger.LogInformation("[SKIP] Eintrag '{Title}' übersprungen (Ausschluss-Filter)", entry.Title);
            return;
        }

        // Proxy anwenden falls konfiguriert
        var finalVideoUrl = entry.VideoUrl;
        if (feedConfig.Proxy == "DE" && !string.IsNullOrEmpty(proxyUrlDe))
        {
            finalVideoUrl = proxyUrlDe + Uri.EscapeDataString(entry.VideoUrl);
            _logger.LogDebug("[PROXY] Video-URL über DE-Proxy geleitet: {Title}", entry.Title);
        }
        else if (feedConfig.Proxy == "AT" && !string.IsNullOrEmpty(proxyUrlAt))
        {
            finalVideoUrl = proxyUrlAt + Uri.EscapeDataString(entry.VideoUrl);
            _logger.LogDebug("[PROXY] Video-URL über AT-Proxy geleitet: {Title}", entry.Title);
        }
        else if (feedConfig.Proxy == "CH" && !string.IsNullOrEmpty(proxyUrlCh))
        {
            finalVideoUrl = proxyUrlCh + Uri.EscapeDataString(entry.VideoUrl);
            _logger.LogDebug("[PROXY] Video-URL über CH-Proxy geleitet: {Title}", entry.Title);
        }

        // Datumspräfix extrahieren
        var datePrefix = RssFeedParser.ExtractPubDate(entry.PublishDate);

        string finalTitle;
        int currentSeason;
        string outputDir = feedBaseDir;

        if (feedConfig.Type == "serie")
        {
            // Titel formatieren und Staffel/Episode extrahieren
            (finalTitle, currentSeason) = TitleExtractor.CleanAndFormatTitle(
                entry.Title,
                feedConfig.Staffel,
                datePrefix,
                _logger);

            // Season-Ordner erstellen
            var seasonFolder = $"Season {currentSeason:D2}";
            outputDir = Path.Combine(feedBaseDir, seasonFolder);
        }
        else
        {
            // Film: KEIN Datum voranstellen (bessere Metadaten-Erkennung in Jellyfin)
            // Duplikate werden automatisch überschrieben (gleiches Publish-Date = gleicher Film)
            finalTitle = entry.Title;
            currentSeason = 1;
        }

        // .strm und .nfo Dateien erstellen
        strmHelper.CreateStrmFile(finalTitle, finalVideoUrl, outputDir);
        strmHelper.CreateNfoFile(finalTitle, entry.Description, entry.PublishDate, outputDir);
    }

    /// <summary>
    /// Prüft ob ein Eintrag ausgeschlossen werden soll.
    /// </summary>
    private bool ShouldExcludeEntry(string title, List<string> excludeList)
    {
        if (excludeList == null || !excludeList.Any())
        {
            return false;
        }

        return excludeList.Any(excludeWord =>
            title.Contains(excludeWord, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Bestimmt das Zielverzeichnis für einen Feed.
    /// </summary>
    private string GetFeedDirectory(string baseOutputDir, FeedConfig feedConfig)
    {
        var contentTypeFolder = feedConfig.Type == "film" ? "Filme" : "Serien";
        var safeFeedName = StrmFileHelper.SanitizeFileName(feedConfig.Name);

        if (feedConfig.Type == "film")
        {
            // Filme: /mediathek/Filme/
            return Path.Combine(baseOutputDir, contentTypeFolder);
        }
        else
        {
            // Serien: /mediathek/Serien/[FeedName]/
            return Path.Combine(baseOutputDir, contentTypeFolder, safeFeedName);
        }
    }

    /// <summary>
    /// Gibt den Pfad zum Jellythek Datenverzeichnis zurück.
    /// </summary>
    private string GetMediathekDataPath()
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || string.IsNullOrWhiteSpace(config.OutputPath))
        {
            // Fallback auf Standard-Pfad
            return "/config/data/jellythek";
        }

        return config.OutputPath;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Läuft täglich um 3 Uhr morgens (leer = manuell konfigurierbar in UI)
        return Array.Empty<TaskTriggerInfo>();
    }
}
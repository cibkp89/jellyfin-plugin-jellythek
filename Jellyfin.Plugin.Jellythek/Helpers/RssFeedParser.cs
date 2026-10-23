using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellythek.Helpers;

/// <summary>
/// Hilfklasse für RSS-Feed-Parsing.
/// </summary>
public class RssFeedParser
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RssFeedParser"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    public RssFeedParser(ILogger logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Erstellt die vollständige RSS-URL basierend auf der Filterabfrage.
    /// </summary>
    /// <param name="filterQuery">Der Suchfilter (z.B. "!orf #maschek").</param>
    /// <returns>Die vollständige RSS-URL.</returns>
    public static string GetFeedUrl(string filterQuery)
    {
        const string baseRssUrl = "https://mediathekviewweb.de/feed?query=";
        var encodedFilter = Uri.EscapeDataString(filterQuery);
        return baseRssUrl + encodedFilter;
    }

    /// <summary>
    /// Lädt und parst einen RSS-Feed.
    /// </summary>
    /// <param name="feedUrl">Die URL des RSS-Feeds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Liste von Feed-Einträgen.</returns>
    public async Task<List<FeedEntry>> ParseFeedAsync(string feedUrl, CancellationToken cancellationToken)
    {
        var entries = new List<FeedEntry>();

        try
        {
            _logger.LogInformation("[API] Rufe RSS-Feed von {Url} ab.", feedUrl);

            var httpClient = _httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync(feedUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var xmlReader = XmlReader.Create(stream);
            
            var feed = SyndicationFeed.Load(xmlReader);

            foreach (var item in feed.Items)
            {
                var entry = new FeedEntry
                {
                    Title = item.Title?.Text ?? string.Empty,
                    PublishDate = item.PublishDate.DateTime,
                    Description = item.Summary?.Text ?? string.Empty
                };

                // Video-URL extrahieren (Priorität wie im Python-Script)
                string? videoUrl = null;

                // Prio 1: Links mit video/* oder application/x-mpegURL MIME-Type
                var videoLink = item.Links.FirstOrDefault(l => 
                    l.MediaType != null && 
                    (l.MediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) || 
                     l.MediaType.Contains("mpegurl", StringComparison.OrdinalIgnoreCase)));

                if (videoLink != null)
                {
                    videoUrl = videoLink.Uri?.ToString();
                }

                // Prio 2: Erster verfügbarer Link als Fallback
                if (string.IsNullOrEmpty(videoUrl) && item.Links.Any())
                {
                    videoUrl = item.Links.First().Uri?.ToString();
                }

                if (!string.IsNullOrEmpty(videoUrl))
                {
                    entry.VideoUrl = videoUrl;
                    entries.Add(entry);
                }
                else
                {
                    _logger.LogWarning("[WARNUNG] Video-URL für '{Title}' nicht gefunden. Eintrag übersprungen.", entry.Title);
                }
            }

            _logger.LogInformation("Gefundene Einträge: {Count}", entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FEHLER] Konnte RSS-Feed nicht abrufen: {Url}", feedUrl);
        }

        return entries;
    }

    /// <summary>
    /// Extrahiert und formatiert das Veröffentlichungsdatum im Format YYYY-MM-DD.
    /// </summary>
    /// <param name="publishDate">Das Veröffentlichungsdatum.</param>
    /// <returns>Formatiertes Datum oder null.</returns>
    public static string? ExtractPubDate(DateTime? publishDate)
    {
        if (publishDate.HasValue)
        {
            return publishDate.Value.ToString("yyyy-MM-dd");
        }

        return null;
    }
}

/// <summary>
/// Repräsentiert einen Eintrag aus einem RSS-Feed.
/// </summary>
public class FeedEntry
{
    /// <summary>
    /// Gets or sets der Titel des Eintrags.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets die Video-URL.
    /// </summary>
    public string VideoUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets das Veröffentlichungsdatum.
    /// </summary>
    public DateTime? PublishDate { get; set; }

    /// <summary>
    /// Gets or sets die Beschreibung.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
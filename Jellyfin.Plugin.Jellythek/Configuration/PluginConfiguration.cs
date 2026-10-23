using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Jellythek.Configuration;

/// <summary>
/// Plugin-Konfiguration für Jellythek.
/// Entspricht der feeds_config.py aus dem Python-Script.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // Standard-Werte
        OutputPath = "/config/data/jellythek";
        ProxyUrlDe = string.Empty;
        ProxyUrlAt = string.Empty;
        ProxyUrlCh = string.Empty;
        SyncEnabled = true;
        Feeds = new List<FeedConfig>();
    }

    /// <summary>
    /// Gets or sets die Liste der RSS-Feeds.
    /// </summary>
    public List<FeedConfig> Feeds { get; set; }

    /// <summary>
    /// Gets or sets den Ausgabepfad für die .strm-Dateien.
    /// </summary>
    public string OutputPath { get; set; }

    /// <summary>
    /// Gets or sets die Proxy-URL für DE-Inhalte.
    /// </summary>
    public string ProxyUrlDe { get; set; }

    /// <summary>
    /// Gets or sets die Proxy-URL für AT-Inhalte.
    /// </summary>
    public string ProxyUrlAt { get; set; }

    /// <summary>
    /// Gets or sets die Proxy-URL für CH-Inhalte.
    /// </summary>
    public string ProxyUrlCh { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether die Synchronisation aktiviert ist.
    /// </summary>
    public bool SyncEnabled { get; set; }
}

/// <summary>
/// Konfiguration für einen einzelnen RSS-Feed.
/// Entspricht einem Dictionary-Eintrag in FEEDS aus feeds_config.py.
/// </summary>
public class FeedConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeedConfig"/> class.
    /// </summary>
    public FeedConfig()
    {
        Name = string.Empty;
        Filter = string.Empty;
        Type = "film";
        Staffel = false;
        MaxEntries = null;
        CleanFolder = false;
        Proxy = null;
        Exclude = new List<string>();
    }

    /// <summary>
    /// Gets or sets der Name des Feeds (z.B. "Sketch-History").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets der Suchfilter (z.B. "!zdf sketch-history >20").
    /// </summary>
    public string Filter { get; set; }

    /// <summary>
    /// Gets or sets den Typ: "serie" oder "film".
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Staffel/Episode aus dem Titel extrahiert werden soll.
    /// Nur relevant bei Type="serie".
    /// </summary>
    public bool Staffel { get; set; }

    /// <summary>
    /// Gets or sets die maximale Anzahl an Einträgen pro Feed.
    /// Null = unbegrenzt.
    /// </summary>
    public int? MaxEntries { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether der Zielordner vor dem Start geleert werden soll.
    /// </summary>
    public bool CleanFolder { get; set; }

    /// <summary>
    /// Gets or sets den Proxy-Modus (z.B. "DE" für deutschen Proxy, "AT" für österreichischen Proxy, "CH" für schweizer Proxy).
    /// Null = kein Proxy.
    /// </summary>
    public string? Proxy { get; set; }

    /// <summary>
    /// Gets or sets die Liste von Wörtern/Phrasen zum Ausschließen von Einträgen.
    /// </summary>
    public List<string> Exclude { get; set; }
}
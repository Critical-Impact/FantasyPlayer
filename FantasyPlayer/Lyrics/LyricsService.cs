using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace FantasyPlayer.Lyrics
{
    public class LyricsService : IDisposable
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly Regex LrcLineRegex = new Regex(@"\[(\d+):(\d+)\.(\d+)\](.*)", RegexOptions.Compiled);

        private readonly IPluginLog _pluginLog;

        public LyricsService(IPluginLog pluginLog)
        {
            _pluginLog = pluginLog;
            HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("FantasyPlayer/1.0");
        }

        public async Task<List<(TimeSpan Time, string Text)>> FetchSyncedLyricsAsync(
            string artist, string title, string album, int durationMs)
        {
            try
            {
                var durationSeconds = durationMs / 1000;
                var url = $"https://lrclib.net/api/get" +
                          $"?artist_name={Uri.EscapeDataString(artist)}" +
                          $"&track_name={Uri.EscapeDataString(title)}" +
                          $"&album_name={Uri.EscapeDataString(album)}" +
                          $"&duration={durationSeconds}";

                var response = await HttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _pluginLog.Debug($"LRCLIB returned {(int)response.StatusCode} for {artist} - {title}");
                    return new List<(TimeSpan, string)>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var lrcResponse = JsonSerializer.Deserialize<LrcLibResponse>(content);
                if (lrcResponse?.SyncedLyrics == null)
                {
                    _pluginLog.Debug($"No synced lyrics found for {artist} - {title}");
                    return new List<(TimeSpan, string)>();
                }

                var lines = ParseLrc(lrcResponse.SyncedLyrics);
                _pluginLog.Debug($"Parsed {lines.Count} lyric lines for {artist} - {title}");
                return lines;
            }
            catch (Exception ex)
            {
                _pluginLog.Error($"Failed to fetch lyrics from LRCLIB: {ex.Message}");
                return new List<(TimeSpan, string)>();
            }
        }

        private static List<(TimeSpan Time, string Text)> ParseLrc(string lrc)
        {
            var lines = new List<(TimeSpan, string)>();
            foreach (var rawLine in lrc.Split('\n'))
            {
                var match = LrcLineRegex.Match(rawLine.Trim());
                if (!match.Success) continue;

                var minutes = int.Parse(match.Groups[1].Value);
                var seconds = int.Parse(match.Groups[2].Value);
                var centiseconds = int.Parse(match.Groups[3].Value);
                var text = match.Groups[4].Value.Trim();

                if (string.IsNullOrWhiteSpace(text)) continue;

                var time = new TimeSpan(0, 0, minutes, seconds, centiseconds * 10);
                lines.Add((time, text));
            }

            return lines;
        }

        public void Dispose()
        {
        }
    }
}

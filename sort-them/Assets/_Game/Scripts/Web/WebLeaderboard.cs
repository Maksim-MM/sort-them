using System.Collections.Generic;
using Playgama;
using Playgama.Modules.Leaderboards;
using UnityEngine;

namespace SortThem.Web
{
    public static class WebLeaderboard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bind()
        {
            Leaderboard.CanSubmit = () => Bridge.leaderboards != null && Bridge.leaderboards.type != LeaderboardType.NotAvailable;
            Leaderboard.CanFetch = () => Bridge.leaderboards != null && Bridge.leaderboards.type == LeaderboardType.InGame;
            Leaderboard.PlayerName = () => Bridge.player != null ? Bridge.player.name : null;
            Leaderboard.Submit = (score, done) => Bridge.leaderboards.SetScore(Leaderboard.Id, score, ok => done?.Invoke(ok));
            Leaderboard.Fetch = done => Bridge.leaderboards.GetEntries(Leaderboard.Id, (ok, list) =>
            {
                if (!ok || list == null) { done?.Invoke(null); return; }
                string me = Bridge.player != null ? Bridge.player.id : null;
                var entries = new List<LeaderboardEntry>(list.Count);
                foreach (var row in list)
                {
                    if (row == null) continue;
                    var entry = new LeaderboardEntry { Name = Get(row, "name") };
                    int.TryParse(Get(row, "rank"), out entry.Rank);
                    if (!int.TryParse(Get(row, "score"), out entry.Score) && double.TryParse(Get(row, "score"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value)) entry.Score = (int)value;
                    string id = Get(row, "id");
                    entry.Me = !string.IsNullOrEmpty(me) && id == me;
                    entries.Add(entry);
                }
                entries.Sort((a, b) => a.Rank != b.Rank && a.Rank > 0 && b.Rank > 0 ? a.Rank.CompareTo(b.Rank) : b.Score.CompareTo(a.Score));
                done?.Invoke(entries);
            });
        }

        static string Get(Dictionary<string, string> row, string key) => row.TryGetValue(key, out var value) ? value : null;
    }
}

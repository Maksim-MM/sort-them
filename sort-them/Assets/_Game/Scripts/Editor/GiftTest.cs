using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SortThem.Editor
{
    public static class GiftTest
    {
        class FakeServices : IPlatformServices
        {
            public bool InterstitialSupported => true;
            public bool RewardedSupported => true;
            public void ShowInterstitial(Action<bool> onDone) => onDone?.Invoke(true);
            public void ShowRewarded(Action<bool> onDone) => onDone?.Invoke(true);
            public bool PaymentsSupported => false;
            public void QueryPurchases(Action<HashSet<string>> onDone) => onDone?.Invoke(null);
            public void QueryProduct(string id, Action<ProductInfo> onDone) => onDone?.Invoke(null);
            public void Purchase(string id, Action<bool> onDone) => onDone?.Invoke(false);
        }

        [MenuItem("SortThem/Test/Gifts: Fake Ads (Play)")]
        public static void FakeAds()
        {
            if (!(Ads.Services is FakeServices)) Ads.Services = new FakeServices();
            Debug.Log("SortThem: fake ads on, rewarded ready in " + Ads.RemainingText());
        }

        [MenuItem("SortThem/Test/Gifts: Fill Nearest Rack (Play)")]
        public static void FillNearestRack()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.Ready) return;
            FakeAds();
            Vector3 origin = gm.Player != null ? gm.Player.transform.position : Vector3.zero;
            RackController rack = null;
            float best = float.MaxValue;
            foreach (var r in UnityEngine.Object.FindObjectsByType<RackController>(FindObjectsSortMode.None))
            {
                if (r.Category == null || r.Shelves == null) continue;
                bool hasEmpty = false;
                foreach (var s in r.Shelves) if (s != null && s.IsEmpty && !s.Locked) hasEmpty = true;
                if (!hasEmpty) continue;
                float d = (r.transform.position - origin).sqrMagnitude;
                if (d < best) { best = d; rack = r; }
            }
            if (rack == null) { Debug.LogWarning("SortThem: no rack with empty shelves"); return; }

            var loose = new Dictionary<CarItemData, List<CarInstance>>();
            foreach (var car in gm.Cars)
            {
                if (car == null || car.Data == null || car.State != CarState.Loose || car.Data.Category != rack.Category) continue;
                if (!loose.TryGetValue(car.Data, out var list)) loose[car.Data] = list = new List<CarInstance>();
                list.Add(car);
            }
            int filled = 0;
            foreach (var shelf in rack.Shelves)
            {
                if (shelf == null || !shelf.IsEmpty || shelf.Locked) continue;
                CarItemData pick = null;
                foreach (var kv in loose) if (kv.Value.Count >= shelf.Capacity) { pick = kv.Key; break; }
                if (pick == null) continue;
                var cars = loose[pick];
                for (int i = 0; i < shelf.Capacity; i++) shelf.TryPlace(cars[i], i, true);
                loose.Remove(pick);
                filled++;
            }
            Debug.Log("SortThem: filled " + filled + " shelves on rack " + rack.name);
        }

        [MenuItem("SortThem/Test/Gifts: Toggle No-Ads Purchase (Play)")]
        public static void TogglePremium()
        {
            Premium.SetOwned(!Premium.Owned);
            Debug.Log("SortThem: premium owned = " + Premium.Owned);
        }

        [MenuItem("SortThem/Test/Daily: Next Day (Play)")]
        public static void NextDay()
        {
            DailyRewards.DayOffset++;
            Debug.Log("SortThem: daily day offset = " + DailyRewards.DayOffset + ", can claim = " + DailyRewards.CanClaim);
        }

        [MenuItem("SortThem/Test/Leaderboard: Fake Data (Play)")]
        public static void FakeLeaderboard()
        {
            int sent = 0;
            string[] names = { "TopDriver", "speed_racer", "Анна", "<b>hack</b>", "", "LongNicknameThatDoesNotFit", "Kenji", "driver77", "Olga", "Sam", "Mia", "Leo" };
            Leaderboard.CanSubmit = () => true;
            Leaderboard.CanFetch = () => true;
            Leaderboard.PlayerName = () => "Maksim";
            Leaderboard.Submit = (score, done) => { sent = score; done?.Invoke(true); };
            Leaderboard.Fetch = done =>
            {
                var list = new List<LeaderboardEntry>();
                int mine = Mathf.Max(sent, Leaderboard.Best);
                for (int i = 0; i < names.Length; i++) list.Add(new LeaderboardEntry { Name = names[i], Score = 2800 - i * 230, Me = false });
                list.Add(new LeaderboardEntry { Name = "Maksim", Score = mine, Me = true });
                list.Sort((a, b) => b.Score.CompareTo(a.Score));
                for (int i = 0; i < list.Count; i++) { var e = list[i]; e.Rank = i + 1; list[i] = e; }
                done?.Invoke(list);
            };
            Debug.Log("SortThem: fake leaderboard on, best = " + Leaderboard.Best);
        }

        [MenuItem("SortThem/Test/Leaderboard: Fake Data (Play)", true)]
        [MenuItem("SortThem/Test/Daily: Next Day (Play)", true)]
        [MenuItem("SortThem/Test/Gifts: Fake Ads (Play)", true)]
        [MenuItem("SortThem/Test/Gifts: Fill Nearest Rack (Play)", true)]
        [MenuItem("SortThem/Test/Gifts: Toggle No-Ads Purchase (Play)", true)]
        static bool InPlay() => Application.isPlaying && GameManager.I != null && GameManager.I.Ready;
    }
}

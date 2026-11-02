using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public enum Trophy
    {
        FiveOfAKind = 1,
        ShelfLife = 2,
        FiftyShelves = 3,
        HalfwayThere = 4,
        WarehouseInOrder = 5,
        CategoryClosed = 6,
        BlueprintCollector = 7,
        RetailTherapy = 8,
        MaxedOut = 9,
        HighRoller = 10,
        Kaboom = 11,
        TreasureHunter = 12,
        MasterMechanic = 13,
    }

    public static class Trophies
    {
        public static event Action<Trophy> Unlocked;

        static readonly HashSet<Trophy> Session = new HashSet<Trophy>();
        static readonly Trophy[] AllTrophies = (Trophy[])Enum.GetValues(typeof(Trophy));

        public static bool Enabled
        {
            get
            {
#if UNITY_PS4 || UNITY_PS5 || UNITY_GAMECORE_XBOXSERIES || UNITY_GAMECORE_XBOXONE || UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static int Count => AllTrophies.Length;

        public static bool Has(Trophy trophy)
        {
            string key = Key(trophy);
            var prefs = SavesReady.Prefs;
            return prefs != null ? prefs.GetBool(key, false) : PlayerPrefs.GetInt(key, 0) != 0;
        }

        public static int UnlockedCount
        {
            get { int n = 0; foreach (var t in AllTrophies) if (Has(t)) n++; return n; }
        }

        public static bool AllUnlocked
        {
            get { foreach (var t in AllTrophies) if (!Has(t)) return false; return true; }
        }

        public static void Unlock(Trophy trophy)
        {
            if (!Enabled || !Session.Add(trophy)) return;
            int id = (int)trophy;
#if UNITY_PS5
            Plugins.UpscaleSDK.PS5.Runtime.Users.Trophies.Ps5Trophies.Unlock(id);
#elif UNITY_PS4
            Plugins.UpscaleSDK.PS4.Runtime.TrophiesManagement.Ps4Trophies.Unlock(id);
#elif UNITY_GAMECORE_XBOXSERIES || UNITY_GAMECORE_XBOXONE
            UpscaleSDK.Platform.Xbox.XboxAchievements.Unlock(id);
#endif
#if UNITY_EDITOR
            Debug.Log("Trophies: unlock " + trophy + " (id " + id + ")");
#endif
            bool first = !Has(trophy);
            if (first)
            {
                string key = Key(trophy);
                var prefs = SavesReady.Prefs;
                if (prefs != null) { prefs.SetBool(key, true); prefs.TrySave(); }
                else { PlayerPrefs.SetInt(key, 1); PlayerPrefs.Save(); }
            }
            Unlocked?.Invoke(trophy);
        }

        public static void Shelves(int closed, int total)
        {
            if (!Enabled || total <= 0) return;
            if (closed >= 1) Unlock(Trophy.ShelfLife);
            if (closed >= 50) Unlock(Trophy.FiftyShelves);
            if (closed * 2 >= total) Unlock(Trophy.HalfwayThere);
            if (closed >= total) Unlock(Trophy.WarehouseInOrder);
        }

        public static void Blueprints(int unlocked, int total)
        {
            if (!Enabled) return;
            if (unlocked >= 1) Unlock(Trophy.CategoryClosed);
            if (total > 0 && unlocked >= total) Unlock(Trophy.BlueprintCollector);
        }

        public static void Upgrades(UpgradeService upgrades, bool purchased)
        {
            if (!Enabled || upgrades == null) return;
            if (purchased) Unlock(Trophy.RetailTherapy);
            if (upgrades.AllMaxed) Unlock(Trophy.MaxedOut);
        }

        public static void Collectibles(int found, int total)
        {
            if (!Enabled || total <= 0) return;
            if (found >= total) Unlock(Trophy.TreasureHunter);
        }

        public static void Hands(IReadOnlyList<CarInstance> items)
        {
            if (!Enabled || items == null || items.Count < 5) return;
            var first = items[0] != null ? items[0].Data : null;
            if (first == null) return;
            foreach (var car in items) if (car == null || car.Data != first) return;
            Unlock(Trophy.FiveOfAKind);
        }

        public static void Evaluate(GameManager gm)
        {
            if (!Enabled || gm == null) return;
            Shelves(gm.ClosedShelves, gm.TotalShelves);
            int blueprints = 0, categories = gm.Catalog != null ? gm.Catalog.Categories.Length : 0;
            for (int i = 0; i < categories; i++) if (gm.HasBlueprint(i)) blueprints++;
            Blueprints(blueprints, categories);
            Upgrades(gm.Upgrades, gm.Upgrades != null && gm.Upgrades.AnyPurchased);
            Collectibles(gm.CollectiblesFound, gm.Collectibles.Count);
            if (gm.Catalog != null)
                foreach (var special in gm.Catalog.Specials)
                    if (special != null && special.Car != null && special.MaxLevel > 0 && gm.SpecialLevel(special.Car) >= special.MaxLevel) Unlock(Trophy.MasterMechanic);
        }

        static string Key(Trophy trophy) => "trophy." + (int)trophy;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public enum Trophy
    {
        FirstSort = 1,
        ShelfLife = 2,
        CategoryClosed = 3,
        RetailTherapy = 4,
        HighRoller = 5,
        FiftyShelves = 6,
        HundredCars = 7,
        HalfwayThere = 8,
        ThousandCars = 9,
        BlueprintCollector = 10,
        MaxedOut = 11,
        MasterMechanic = 12,
        Collector = 13,
        WarehouseInOrder = 14,
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
            if (closed >= 150) Unlock(Trophy.HalfwayThere);
            if (closed >= total) Unlock(Trophy.WarehouseInOrder);
        }

        public static void Cars(int placed)
        {
            if (!Enabled) return;
            if (placed >= 1) Unlock(Trophy.FirstSort);
            if (placed >= 100) Unlock(Trophy.HundredCars);
            if (placed >= 1000) Unlock(Trophy.ThousandCars);
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
            if (upgrades.TerminalMaxed) Unlock(Trophy.MaxedOut);
        }

        public static void Specials(GameManager gm)
        {
            if (!Enabled || gm == null || gm.Catalog == null) return;
            int total = 0, maxed = 0;
            foreach (var special in gm.Catalog.Specials)
            {
                if (special == null || special.Car == null || special.MaxLevel <= 0) continue;
                total++;
                if (gm.SpecialLevel(special.Car) >= special.MaxLevel) maxed++;
            }
            if (maxed >= 1) Unlock(Trophy.MasterMechanic);
            if (total > 0 && maxed >= total) Unlock(Trophy.Collector);
        }

        public static void Evaluate(GameManager gm)
        {
            if (!Enabled || gm == null) return;
            Shelves(gm.ClosedShelves, gm.TotalShelves);
            Cars(gm.PlacedValid);
            Upgrades(gm.Upgrades, gm.Upgrades != null && gm.Upgrades.AnyPurchased);
            Specials(gm);
        }

        static string Key(Trophy trophy) => "trophy." + (int)trophy;
    }
}

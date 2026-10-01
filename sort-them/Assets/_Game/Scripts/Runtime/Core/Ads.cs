using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortThem
{
    public interface IPlatformServices
    {
        bool InterstitialSupported { get; }
        bool RewardedSupported { get; }
        void ShowInterstitial(Action<bool> onDone);
        void ShowRewarded(Action<bool> onDone);
        bool PaymentsSupported { get; }
        void QueryPurchases(Action<HashSet<string>> onDone);
        void QueryProduct(string id, Action<ProductInfo> onDone);
        void Purchase(string id, Action<bool> onDone);
    }

    public class NullPlatformServices : IPlatformServices
    {
        public bool InterstitialSupported => false;
        public bool RewardedSupported => false;
        public void ShowInterstitial(Action<bool> onDone) => onDone?.Invoke(false);
        public void ShowRewarded(Action<bool> onDone) => onDone?.Invoke(false);
        public bool PaymentsSupported => false;
        public void QueryPurchases(Action<HashSet<string>> onDone) => onDone?.Invoke(null);
        public void QueryProduct(string id, Action<ProductInfo> onDone) => onDone?.Invoke(null);
        public void Purchase(string id, Action<bool> onDone) => onDone?.Invoke(false);
    }

    public static class Ads
    {
        const float BusyTimeout = 90f;

        public static IPlatformServices Services = new NullPlatformServices();
        public static bool NoInterstitials;
        public static bool NoRewarded;

        static float _lastAd = float.NegativeInfinity;
        static float _busyUntil = float.NegativeInfinity;

        static GameConfig Cfg => GameManager.I != null ? GameManager.I.Config : null;
        static float Cooldown => Cfg != null ? Cfg.AdCooldown : 180f;
        static float InitialDelay => Cfg != null ? Cfg.AdInitialDelay : 90f;
        static float Discount => Cfg != null ? Cfg.RewardedDiscount : 0.25f;

        public static bool Busy => Time.realtimeSinceStartup < _busyUntil;
        public static bool RewardedAvailable => !NoRewarded && Services.RewardedSupported;
        public static int DiscountPercent => Mathf.RoundToInt(Discount * 100f);

        public static float Remaining
        {
            get
            {
                float now = Time.realtimeSinceStartup;
                return Mathf.Max(0f, Mathf.Max(Cooldown - (now - _lastAd), InitialDelay - now));
            }
        }

        public static bool Ready => !Busy && Remaining <= 0f;

        public static int Discounted(int cost) => Mathf.Max(0, Mathf.CeilToInt(cost * (1f - Discount)));

        public static string RemainingText()
        {
            int s = Mathf.CeilToInt(Remaining);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        public static void TryInterstitial()
        {
            if (NoInterstitials || !Services.InterstitialSupported || !Ready) return;
            _busyUntil = Time.realtimeSinceStartup + BusyTimeout;
            Services.ShowInterstitial(shown =>
            {
                _busyUntil = float.NegativeInfinity;
                if (shown) _lastAd = Time.realtimeSinceStartup;
            });
        }

        public static void ShowRewarded(Action<bool> onDone)
        {
            if (!RewardedAvailable || !Ready) { onDone?.Invoke(false); return; }
            _busyUntil = Time.realtimeSinceStartup + BusyTimeout;
            Services.ShowRewarded(rewarded =>
            {
                _busyUntil = float.NegativeInfinity;
                if (rewarded) _lastAd = Time.realtimeSinceStartup;
                onDone?.Invoke(rewarded);
            });
        }
    }
}

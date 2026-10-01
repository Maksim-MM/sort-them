using System;
using System.Collections;
using System.Collections.Generic;
using Playgama;
using Playgama.Modules.Advertisement;
using UnityEngine;
using UnityEngine.Networking;

namespace SortThem.Web
{
    public class WebAds : IPlatformServices
    {
        public bool InterstitialSupported => Bridge.advertisement != null && Bridge.advertisement.isInterstitialSupported;
        public bool RewardedSupported => Bridge.advertisement != null && Bridge.advertisement.isRewardedSupported;

        public void ShowInterstitial(Action<bool> onDone)
        {
            var adv = Bridge.advertisement;
            if (adv == null) { onDone?.Invoke(false); return; }
            bool opened = false;
            Action<InterstitialState> handler = null;
            handler = state =>
            {
                if (state == InterstitialState.Opened) { opened = true; return; }
                if (state != InterstitialState.Closed && state != InterstitialState.Failed) return;
                adv.interstitialStateChanged -= handler;
                onDone?.Invoke(opened && state == InterstitialState.Closed);
            };
            adv.interstitialStateChanged += handler;
            adv.ShowInterstitial();
        }

        public void ShowRewarded(Action<bool> onDone)
        {
            var adv = Bridge.advertisement;
            if (adv == null) { onDone?.Invoke(false); return; }
            bool rewarded = false;
            Action<RewardedState> handler = null;
            handler = state =>
            {
                if (state == RewardedState.Rewarded) { rewarded = true; return; }
                if (state != RewardedState.Closed && state != RewardedState.Failed) return;
                adv.rewardedStateChanged -= handler;
                onDone?.Invoke(rewarded);
            };
            adv.rewardedStateChanged += handler;
            adv.ShowRewarded();
        }

        public bool PaymentsSupported => Bridge.payments != null && Bridge.payments.isSupported;

        public void QueryPurchases(Action<HashSet<string>> onDone)
        {
            Bridge.payments.GetPurchases((ok, list) =>
            {
                if (!ok || list == null) { onDone?.Invoke(null); return; }
                var ids = new HashSet<string>();
                foreach (var p in list) if (p != null && p.TryGetValue("id", out var id) && !string.IsNullOrEmpty(id)) ids.Add(id);
                onDone?.Invoke(ids);
            });
        }

        public void QueryProduct(string id, Action<ProductInfo> onDone)
        {
            Bridge.payments.GetCatalog((ok, list) =>
            {
                if (!ok || list == null) { onDone?.Invoke(null); return; }
                foreach (var item in list)
                {
                    if (item == null || !item.TryGetValue("id", out var pid) || pid != id) continue;
                    var info = new ProductInfo { Id = id, Title = Get(item, "title"), Price = Get(item, "price"), CurrencyCode = Get(item, "priceCurrencyCode") };
                    if (string.IsNullOrEmpty(info.Price))
                    {
                        string value = Get(item, "priceValue");
                        if (!string.IsNullOrEmpty(value)) info.Price = (value + " " + (info.CurrencyCode ?? "")).Trim();
                    }
                    string img = Get(item, "priceCurrencyImage");
                    if (!string.IsNullOrEmpty(img) && WebBridge.Instance != null) WebBridge.Instance.StartCoroutine(LoadIcon(img, info, onDone));
                    else onDone?.Invoke(info);
                    return;
                }
                onDone?.Invoke(null);
            });
        }

        public void Purchase(string id, Action<bool> onDone)
        {
            Bridge.payments.Purchase(id, (ok, data) => onDone?.Invoke(ok));
        }

        static string Get(Dictionary<string, string> d, string key) => d.TryGetValue(key, out var v) ? v : null;

        static IEnumerator LoadIcon(string url, ProductInfo info, Action<ProductInfo> onDone)
        {
            using (var req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) info.CurrencyIcon = DownloadHandlerTexture.GetContent(req);
                else Debug.LogWarning("SortThem: currency icon load failed: " + req.error);
            }
            onDone?.Invoke(info);
        }
    }
}

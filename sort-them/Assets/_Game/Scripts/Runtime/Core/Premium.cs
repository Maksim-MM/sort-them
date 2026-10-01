using System;
using UnityEngine;

namespace SortThem
{
    public class ProductInfo
    {
        public string Id, Title, Price, CurrencyCode;
        public Texture2D CurrencyIcon;
    }

    public static class Premium
    {
        public const string ProductId = "no_ads_discount";

        public static bool Owned { get; private set; }
        public static ProductInfo Product { get; private set; }
        public static event Action Changed;

        static GameConfig Cfg => GameManager.I != null ? GameManager.I.Config : null;
        static float Discount => Cfg != null ? Cfg.PremiumDiscount : 0.5f;

        public static int DiscountPercent => Mathf.RoundToInt(Discount * 100f);
        public static bool Supported => Ads.Services.PaymentsSupported;
        public static bool CanOffer => Supported && !Owned && Product != null;

        public static int Price(int cost) => Owned ? Mathf.Max(0, Mathf.CeilToInt(cost * (1f - Discount))) : cost;

        public static void SetOwned(bool owned)
        {
            if (Owned == owned) return;
            Owned = owned;
            Ads.NoInterstitials = owned;
            Ads.NoRewarded = owned;
            Changed?.Invoke();
        }

        public static void SetProduct(ProductInfo product)
        {
            Product = product;
            Changed?.Invoke();
        }

        public static void Refresh()
        {
            if (!Supported) return;
            Ads.Services.QueryPurchases(ids => { if (ids != null && ids.Contains(ProductId)) SetOwned(true); });
            Ads.Services.QueryProduct(ProductId, SetProduct);
        }

        public static void Buy(Action<bool> onDone)
        {
            if (!CanOffer) { onDone?.Invoke(false); return; }
            Ads.Services.Purchase(ProductId, ok =>
            {
                if (ok) SetOwned(true);
                onDone?.Invoke(ok);
            });
        }
    }
}

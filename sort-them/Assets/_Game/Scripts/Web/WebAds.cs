using System;
using Playgama;
using Playgama.Modules.Advertisement;

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
    }
}

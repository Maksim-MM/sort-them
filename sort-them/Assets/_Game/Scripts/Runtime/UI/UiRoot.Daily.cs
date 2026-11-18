using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public partial class UiRoot
    {
        class DailyCell
        {
            public RectTransform Root;
            public Image Border;
            public TMP_Text Day, Value, Note;
            public GameObject Tick;
        }

        static readonly Color DailyDone = new Color(0.3f, 0.33f, 0.38f, 1f);
        static readonly Color DailyDoneText = new Color(0.5f, 0.53f, 0.58f, 1f);

        RectTransform _daily;
        TMP_Text _dailyTitle, _dailyButtonLabel;
        bool _dailyShown;
        readonly List<DailyCell> _dailyCells = new List<DailyCell>();
        readonly List<Selectable> _dailyButtons = new List<Selectable>();

        public bool DailyOpen => _daily != null && _daily.gameObject.activeSelf;

        void BuildDaily()
        {
            const float screenH = 190f, pad = 12f;
            var w = BuildArcadeWindow("Daily", new Vector2(1040f, ArcadeHeight(screenH, false, true)), "ui.pause", "", false, true);
            _daily = w.Root;
            _dailyTitle = w.Title;
            _bound.RemoveAll(b => b.Text == w.Title);
            int count = DailyRewards.Rewards.Length;
            for (int i = 0; i < count; i++)
            {
                var fill = UiFactory.NeonBox(w.Screen, "Day" + (i + 1), ArcadeNeon, new Color(0.06f, 0.10f, 0.14f, 1f), 3f);
                var root = (RectTransform)fill.parent;
                UiFactory.Anchor(root, new Vector2(i / (float)count, 0f), new Vector2((i + 1) / (float)count, 1f), new Vector2(i == 0 ? pad : pad * 0.5f, pad), new Vector2(i == count - 1 ? -pad : -pad * 0.5f, -pad));
                var cell = new DailyCell { Root = root, Border = root.GetComponent<Image>() };
                cell.Day = UiFactory.Text(fill, "Day", (i + 1).ToString(), 20f, TextAlignmentOptions.Center, Color.white);
                cell.Day.fontStyle = FontStyles.Bold;
                UiFactory.Anchor(cell.Day.rectTransform, new Vector2(0f, 0.74f), Vector2.one, Vector2.zero, Vector2.zero);
                cell.Value = UiFactory.Text(fill, "Value", "", 34f, TextAlignmentOptions.Center, Color.white);
                cell.Value.fontStyle = FontStyles.Bold;
                cell.Value.enableAutoSizing = true;
                cell.Value.fontSizeMin = 16f;
                cell.Value.fontSizeMax = 34f;
                UiFactory.Anchor(cell.Value.rectTransform, new Vector2(0f, 0.32f), new Vector2(1f, 0.74f), new Vector2(6f, 0f), new Vector2(-6f, 0f));
                cell.Note = UiFactory.Text(fill, "Note", "", 14f, TextAlignmentOptions.Center, Color.white);
                cell.Note.enableAutoSizing = true;
                cell.Note.fontSizeMin = 8f;
                cell.Note.fontSizeMax = 14f;
                cell.Note.textWrappingMode = TextWrappingModes.Normal;
                UiFactory.Anchor(cell.Note.rectTransform, new Vector2(0f, 0.04f), new Vector2(1f, 0.32f), new Vector2(6f, 0f), new Vector2(-6f, 0f));
                cell.Tick = new GameObject("Tick", typeof(RectTransform));
                var tick = (RectTransform)cell.Tick.transform;
                tick.SetParent(fill, false);
                UiFactory.Anchored(tick, new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(26f, 26f));
                var green = new Color(0.45f, 1f, 0.5f, 1f);
                var shortBar = UiFactory.Image(tick, "Short", null, green, Image.Type.Simple);
                UiFactory.Anchored(shortBar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-5.5f, -3f), new Vector2(4f, 11f));
                shortBar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                var longBar = UiFactory.Image(tick, "Long", null, green, Image.Type.Simple);
                UiFactory.Anchored(longBar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(3f, 0.5f), new Vector2(4f, 20f));
                longBar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -40f);
                cell.Tick.SetActive(false);
                _dailyCells.Add(cell);
            }
            FinishArcadeScreen(w.Screen);
            var claim = ArcadePanelButton(w, "Claim", ClaimDaily, ArcadeCost, 300f, true, 20f, true);
            _dailyButtonLabel = claim.GetComponentInChildren<TMP_Text>();
            var rt = claim.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            _dailyButtons.Add(claim);
            _daily.gameObject.SetActive(false);
            BuildCoupon();
        }

        UiCoupon _coupon;

        void BuildCoupon()
        {
            if (_terminal == null) return;
            var ink = new Color(0.28f, 0.17f, 0.02f, 1f);
            var root = UiFactory.Rect(_terminal, "Coupon");
            root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(170f, 64f);
            root.anchoredPosition = new Vector2(-(ArcadeSide + 24f + 85f), -(ArcadeScreenTop + ArcadeBalanceHeight * 0.5f));

            var card = UiFactory.Rect(root, "Card");
            UiFactory.Anchor(card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var body = UiFactory.Image(card, "Body", null, Gold, Image.Type.Simple);
            UiFactory.Anchor(body.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            body.gameObject.AddComponent<Mask>();
            var frame = UiFactory.NeonBox(body.transform, "Frame", ink, Gold, 2f);
            UiFactory.Anchor((RectTransform)frame.parent, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            var glint = UiFactory.Image(body.transform, "Glint", null, new Color(1f, 1f, 1f, 0.5f), Image.Type.Simple);
            UiFactory.Anchored(glint.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 160f));
            glint.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            var text = UiFactory.Text(body.transform, "Text", "-" + DailyRewards.DiscountPercent + "%", 32f, TextAlignmentOptions.Center, ink);
            text.fontStyle = FontStyles.Bold;
            UiFactory.Anchor(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            for (int i = 0; i < 2; i++)
            {
                var notch = UiFactory.Image(card, "Notch" + i, null, ArcadeBody, Image.Type.Simple);
                UiFactory.Anchored(notch.rectTransform, new Vector2(i, 0.5f), Vector2.zero, new Vector2(16f, 16f));
                notch.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                notch.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            _coupon = root.gameObject.AddComponent<UiCoupon>();
            _coupon.Card = card;
            _coupon.Group = group;
            _coupon.Glint = glint.rectTransform;
            _coupon.Set(DailyRewards.Discount);
            if (!DailyRewards.Discount) card.gameObject.SetActive(false);
        }

        void UpdateDaily()
        {
            if (_daily == null || AnyOpen) return;
            var gm = GameManager.I;
            if (gm == null || !gm.Ready || !gm.TutorialDone || Tutorial.Running || Ads.Busy) return;
            if (!DailyRewards.Enabled) return;
            if (!DailyRewards.CanClaim && _dailyShown) return;
            _dailyShown = true;
            RefreshDaily();
            _daily.gameObject.SetActive(true);
            _focused = FirstCandidate(_dailyButtons);
        }

        void RefreshDaily()
        {
            bool claimable = DailyRewards.CanClaim;
            int count = DailyRewards.Rewards.Length;
            int today = DailyRewards.Index;
            if (!claimable && today == 0) today = count;
            _dailyTitle.text = (claimable ? today + 1 : today) + " / " + count;
            _dailyButtonLabel.text = claimable ? Loc.Get("ctl.interact", "Взять") : Loc.Get("ui.close", "Закрыть");
            for (int i = 0; i < _dailyCells.Count; i++)
            {
                var cell = _dailyCells[i];
                var reward = DailyRewards.Rewards[i];
                bool done = i < today, now = claimable && i == today;
                cell.Tick.SetActive(done);
                var text = done ? DailyDoneText : now ? ArcadeFocus : Color.white;
                cell.Border.color = done ? DailyDone : now ? ArcadeFocus : ArcadeNeon;
                cell.Root.localScale = Vector3.one * (now ? 1.06f : 1f);
                cell.Day.color = text;
                cell.Value.color = text;
                cell.Note.color = text;
                cell.Value.text = DailyValue(reward, done);
                cell.Note.text = DailyNote(reward, done);
            }
        }

        static string DailyValue(DailyReward reward, bool revealed)
        {
            switch (reward.Kind)
            {
                case DailyKind.Money: return "$" + reward.Amount;
                case DailyKind.Spin: return "-100%";
                case DailyKind.Discount: return "-" + DailyRewards.DiscountPercent + "%";
                default: return revealed ? "$" + reward.Amount : "?";
            }
        }

        static string DailyNote(DailyReward reward, bool revealed)
        {
            switch (reward.Kind)
            {
                case DailyKind.Spin: return Loc.Get("ui.slot", "Слот-машина");
                case DailyKind.Discount: return Loc.Get("ui.terminal", "Терминал улучшений");
                default: return "";
            }
        }

        void ClaimDaily()
        {
            var gm = GameManager.I;
            _daily.gameObject.SetActive(false);
            ClearFocus();
            if (gm == null || !DailyRewards.Claim(gm, out var reward)) return;
            Sfx.PlayUi(gm.Config.PurchaseClip);
            string note = DailyNote(reward, true);
            Messages.Show(note.Length > 0 ? note + " · " + DailyValue(reward, true) : "+$" + reward.Amount);
            RefreshTerminal();
            RefreshSlot();
            gm.Save.SaveNow("daily");
        }

        int TerminalCost(UpgradeData data)
        {
            int cost = GameManager.I.Upgrades.NextCost(data);
            return DailyRewards.Discount ? DailyRewards.DiscountPrice(cost) : cost;
        }

        void BuyUpgrade(UpgradeData data)
        {
            var gm = GameManager.I;
            if (gm == null) return;
            bool daily = DailyRewards.Discount;
            if (!gm.Upgrades.TryBuy(data, TerminalCost(data))) return;
            if (daily) DailyRewards.UseDiscount();
            Sfx.PlayUi(gm.Config.PurchaseClip);
            gm.Save.SaveNow("purchase");
            RefreshTerminal();
        }
    }
}

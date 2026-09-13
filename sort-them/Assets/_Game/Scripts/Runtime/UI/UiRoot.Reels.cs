using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public partial class UiRoot
    {
        class Reel
        {
            public RectTransform Strip;
            public Image[] Cells;
            public float Offset;
            public float Speed;
            public float StopAt;
            public float TargetOffset;
            public int TargetIndex;
            public bool Settling;
        }

        readonly Reel[] _reels = new Reel[3];
        readonly List<Sprite> _reelSprites = new List<Sprite>();
        RectTransform _prizeList;
        readonly List<(Image Icon, TMP_Text Count, UpgradeData Data)> _prizeRows = new List<(Image, TMP_Text, UpgradeData)>();

        const float CellSize = 104f, ReelSpeed = 9f, ReelStagger = 0.32f, SettleTime = 0.45f;

        void BuildReels(RectTransform parent)
        {
            CollectReelSprites();
            for (int i = 0; i < _reels.Length; i++)
            {
                float w = 1f / _reels.Length;
                var frame = UiFactory.NeonBox(parent, "Reel" + i, ArcadeNeon, new Color(0.93f, 0.94f, 0.96f, 1f), 3f);
                var frameRoot = (RectTransform)frame.parent;
                UiFactory.Anchor(frameRoot, new Vector2(w * i, 0f), new Vector2(w * (i + 1), 1f), new Vector2(8f, 8f), new Vector2(-8f, -8f));
                frame.gameObject.AddComponent<RectMask2D>();

                var strip = UiFactory.Rect(frame, "Strip");
                UiFactory.Anchor(strip, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
                strip.sizeDelta = new Vector2(0f, CellSize * 3f);

                var reel = new Reel { Strip = strip, Cells = new Image[4] };
                for (int c = 0; c < reel.Cells.Length; c++)
                {
                    var cell = UiFactory.Image(strip, "Cell" + c, null, Color.black, Image.Type.Simple);
                    cell.preserveAspect = true;
                    UiFactory.Anchored(cell.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, (1 - c) * CellSize), new Vector2(CellSize * 0.72f, CellSize * 0.72f));
                    reel.Cells[c] = cell;
                }
                reel.Offset = i * 2.4f;
                _reels[i] = reel;
                PaintReel(reel);
            }
        }

        void CollectReelSprites()
        {
            _reelSprites.Clear();
            var gm = GameManager.I;
            if (gm == null) return;
            foreach (var u in gm.Upgrades.All)
                if (u.Source == UpgradeSource.Slot && u.Icon != null) _reelSprites.Add(u.Icon);
            if (gm.Config.BombPrefab != null && BombIcon != null) _reelSprites.Add(BombIcon);
        }

        int ReelIndexOf(Sprite sprite)
        {
            for (int i = 0; i < _reelSprites.Count; i++) if (_reelSprites[i] == sprite) return i;
            return 0;
        }

        static int Mod(int a, int m) => m <= 0 ? 0 : ((a % m) + m) % m;

        void PaintReel(Reel reel)
        {
            if (_reelSprites.Count == 0) return;
            int baseIndex = Mathf.FloorToInt(reel.Offset);
            float frac = reel.Offset - baseIndex;
            for (int c = 0; c < reel.Cells.Length; c++)
            {
                var sprite = _reelSprites[Mod(baseIndex + 1 - c, _reelSprites.Count)];
                reel.Cells[c].sprite = sprite;
                reel.Cells[c].enabled = sprite != null;
                var rt = reel.Cells[c].rectTransform;
                rt.anchoredPosition = new Vector2(0f, (1 - c + frac) * CellSize);
            }
        }

        void StartReels(Sprite target)
        {
            if (_reelSprites.Count == 0) CollectReelSprites();
            if (_reelSprites.Count == 0) return;
            int index = ReelIndexOf(target);
            float now = Time.unscaledTime;
            for (int i = 0; i < _reels.Length; i++)
            {
                var reel = _reels[i];
                if (reel == null) continue;
                reel.Speed = ReelSpeed + i * 1.5f;
                reel.StopAt = now + SpinDuration + i * ReelStagger;
                reel.TargetIndex = index;
                reel.Settling = false;
            }
        }

        void UpdateReels()
        {
            int n = _reelSprites.Count;
            if (n == 0) return;
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            for (int i = 0; i < _reels.Length; i++)
            {
                var reel = _reels[i];
                if (reel == null || reel.Speed <= 0f) continue;
                if (!reel.Settling)
                {
                    reel.Offset += reel.Speed * dt;
                    if (now >= reel.StopAt)
                    {
                        int curFloor = Mathf.FloorToInt(reel.Offset);
                        int steps = Mod(reel.TargetIndex - curFloor, n);
                        if (steps < 3) steps += n;
                        reel.TargetOffset = curFloor + steps;
                        reel.Settling = true;
                    }
                }
                else
                {
                    float left = reel.TargetOffset - reel.Offset;
                    float step = Mathf.Max(left / SettleTime, 1.2f) * dt;
                    reel.Offset = Mathf.Min(reel.TargetOffset, reel.Offset + step);
                    if (reel.TargetOffset - reel.Offset <= 0.0005f)
                    {
                        reel.Offset = reel.TargetOffset;
                        reel.Speed = 0f;
                        reel.Settling = false;
                        var gm = GameManager.I;
                        if (gm != null) Sfx.PlayUi(gm.Config.SlotReelClip);
                    }
                }
                PaintReel(reel);
            }
        }

        bool ReelsRunning()
        {
            foreach (var r in _reels) if (r != null && r.Speed > 0f) return true;
            return false;
        }

        void BuildPrizeList(RectTransform parent)
        {
            _prizeList = UiFactory.Rect(parent, "Prizes");
            UiFactory.Anchor(_prizeList, Vector2.zero, Vector2.one, new Vector2(6f, 8f), new Vector2(-6f, -40f));
            var grid = _prizeList.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112f, 58f);
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(6, 6, 4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            var caption = Bind(UiFactory.Text(parent, "PrizesCaption", "", 20f, TextAlignmentOptions.Center, ArcadeNeon), "ui.slot_prizes", "Призы");
            caption.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            UiFactory.Anchor(caption.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, -36f), new Vector2(-4f, -8f));

            var gm = GameManager.I;
            if (gm == null) return;
            foreach (var u in gm.Upgrades.All)
            {
                if (u.Source != UpgradeSource.Slot) continue;
                var row = UiFactory.Rect(_prizeList, "Prize_" + u.UpgradeID);
                                var icon = UiFactory.Image(row, "Icon", u.Icon, Color.white, Image.Type.Simple);
                icon.preserveAspect = true;
                UiFactory.Anchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(44f, 44f));
                var count = UiFactory.Text(row, "Count", "", 28f, TextAlignmentOptions.Right, ArcadeCost);
                UiFactory.Anchor(count.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(50f, 0f), new Vector2(-4f, 0f));
                _prizeRows.Add((icon, count, u));
            }
        }

        void RefreshPrizes()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            foreach (var (icon, count, data) in _prizeRows)
            {
                int left = Mathf.Max(0, data.MaxLevel - gm.Upgrades.Level(data));
                count.text = "x" + left;
                var c = left > 0 ? Color.white : new Color(1f, 1f, 1f, 0.25f);
                icon.color = c;
                count.color = left > 0 ? ArcadeCost : new Color(1f, 1f, 1f, 0.25f);
            }
        }
    }
}

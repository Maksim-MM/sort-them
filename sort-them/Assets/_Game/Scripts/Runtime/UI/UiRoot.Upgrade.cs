using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public partial class UiRoot
    {
        RectTransform _upgrade, _confirm;
        Image _upgradeBackdrop;
        RawImage _upgradeView;
        UiDragArea _upgradeDrag;
        TMP_Text _upgradeTitle, _upgradeLevel, _upgradeNext, _upgradeCrates, _holdLabel, _hoverText;
        UiHoldButton _holdButton;
        Image _holdFill;
        readonly List<Selectable> _upgradeButtons = new List<Selectable>();
        readonly List<Selectable> _confirmButtons = new List<Selectable>();
        CarInstance _upgradeCar;
        Camera _previewCam;
        RenderTexture _previewRt;
        Transform _stage, _stageCar;
        float _stageYaw = 145f, _holdProgress, _stageDistance = 1f;
        bool _holdLatched;
        Vector3 _stageCenter;
        Material _goldOutline;
        PlayerInteraction _interaction;
        const int PreviewLayer = 11;
        const float PreviewFov = 28f, PreviewElevation = 16f, DragDegreesPerPixel = 0.35f;

        public bool UpgradeOpen => _upgrade != null && _upgrade.gameObject.activeSelf;
        public bool ConfirmOpen => _confirm != null && _confirm.gameObject.activeSelf;

        void BuildUpgrade()
        {
            var gm = GameManager.I;
            _upgrade = UiFactory.Panel(transform, "Upgrade", new Color(0.05f, 0.05f, 0.07f, 1f));
            UiFactory.Anchor(_upgrade, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _upgradeBackdrop = _upgrade.GetComponent<Image>();
            if (gm != null && gm.Config.UpgradeBackdrop != null)
            {
                _upgradeBackdrop.sprite = gm.Config.UpgradeBackdrop;
                _upgradeBackdrop.type = Image.Type.Simple;
                _upgradeBackdrop.preserveAspect = false;
                _upgradeBackdrop.color = new Color(0.8f, 0.8f, 0.85f, 1f);
            }

            _previewRt = new RenderTexture(1024, 768, 16, RenderTextureFormat.ARGB32) { name = "UpgradePreview" };
            var viewRt = UiFactory.Rect(_upgrade, "View");
            UiFactory.Anchor(viewRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-520f, -270f), new Vector2(520f, 310f));
            _upgradeView = viewRt.gameObject.AddComponent<RawImage>();
            _upgradeView.texture = _previewRt;
            _upgradeView.color = Color.white;
            _upgradeView.raycastTarget = true;
            _upgradeDrag = viewRt.gameObject.AddComponent<UiDragArea>();

            var marquee = UiFactory.NeonBox(_upgrade, "Marquee", ArcadeNeon, new Color(0.05f, 0.07f, 0.11f, 1f), 4f);
            UiFactory.Anchored((RectTransform)marquee.parent, new Vector2(0.5f, 1f), new Vector2(0f, -ArcadeMarqueeTop), new Vector2(760f, ArcadeMarqueeHeight));
            UiFactory.Glow((RectTransform)marquee.parent, 24f, 0.5f);
            _upgradeTitle = UiFactory.Text(marquee, "Title", "", 40f, TextAlignmentOptions.Center, ArcadeTitle);
            _upgradeTitle.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            _upgradeTitle.characterSpacing = 4f;
            UiFactory.TextGlow(_upgradeTitle, new Color(ArcadeTitle.r, ArcadeTitle.g, ArcadeTitle.b, 0.85f), 0.3f, 0.6f);
            UiFactory.Anchor(_upgradeTitle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _upgradeLevel = UiFactory.Text(_upgrade, "Level", "", 30f, TextAlignmentOptions.Center, ArcadeCost);
            _upgradeLevel.fontStyle = FontStyles.Bold;
            UiFactory.TextGlow(_upgradeLevel, new Color(ArcadeCost.r, ArcadeCost.g, ArcadeCost.b, 0.7f), 0.2f, 0.5f);
            UiFactory.Anchored(_upgradeLevel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -(ArcadeMarqueeTop + ArcadeMarqueeHeight + 22f)), new Vector2(900f, 40f));
            _upgradeNext = UiFactory.Text(_upgrade, "Next", "", 22f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f, 1f));
            UiFactory.Anchored(_upgradeNext.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 32f));

            var close = Bind(ArcadeButton(_upgrade, "Close", CloseUpgrade, ArcadeNeon, 18f), "ui.close", "Закрыть");
            UiFactory.Anchored(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-ArcadeButtonInset, -ArcadeMarqueeTop - 8f), new Vector2(150f, 56f));
            _upgradeButtons.Add(close);

            var hold = UiFactory.Panel(_upgrade, "Hold", ArcadeCost);
            UiFactory.Anchored(hold, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(360f, 72f));
            var holdInner = UiFactory.Panel(hold, "Fill", ArcadeFill(ArcadeCost));
            UiFactory.Anchor(holdInner, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            holdInner.GetComponent<Image>().raycastTarget = false;
            _holdFill = UiFactory.Image(holdInner, "Progress", Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)), new Color(ArcadeCost.r, ArcadeCost.g, ArcadeCost.b, 0.85f), Image.Type.Filled);
            _holdFill.fillMethod = Image.FillMethod.Horizontal;
            _holdFill.fillOrigin = 0;
            _holdFill.fillAmount = 0f;
            UiFactory.Anchor(_holdFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _holdButton = hold.gameObject.AddComponent<UiHoldButton>();
            _holdButton.targetGraphic = hold.GetComponent<Image>();
            _holdButton.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = _holdButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            _holdButton.colors = colors;
            _holdLabel = UiFactory.Text(holdInner, "Label", "", 22f, TextAlignmentOptions.Center, ArcadeCost);
            _holdLabel.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            UiFactory.TextGlow(_holdLabel, new Color(ArcadeCost.r, ArcadeCost.g, ArcadeCost.b, 0.6f), 0.2f, 0.5f);
            UiFactory.Anchor(_holdLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.Glow(hold, 16f, 0.55f);
            _arcadeIdle[_holdButton] = ArcadeCost;
            _upgradeButtons.Insert(0, _holdButton);

            _upgradeCrates = UiFactory.Text(_upgrade, "Crates", "", 24f, TextAlignmentOptions.Center, Color.white);
            UiFactory.Anchored(_upgradeCrates.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -(ArcadeMarqueeTop + ArcadeMarqueeHeight + 62f)), new Vector2(600f, 34f));

            _upgrade.gameObject.SetActive(false);

            _hoverText = UiFactory.Text(_safe, "HoverInfo", "", 22f, TextAlignmentOptions.Top, Gold);
            _hoverText.fontStyle = FontStyles.Bold;
            UiFactory.Anchored(_hoverText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(600f, 60f));
        }

        void BuildConfirm()
        {
            const float screenH = 130f;
            var w = BuildArcadeWindow("Confirm", new Vector2(660f, ArcadeHeight(screenH, false, true)), "ui.newgame", "Сбросить прогресс", false, true);
            _confirm = w.Root;
            var text = Bind(UiFactory.Text(w.Screen, "Text", "", 24f, TextAlignmentOptions.Center, Color.white), "ui.reset_confirm", "Вы точно хотите сбросить весь прогресс и начать заново?");
            text.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.Anchor(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 12f), new Vector2(-28f, -12f));
            FinishArcadeScreen(w.Screen);
            var no = Bind(ArcadePanelButton(w, "No", CloseConfirm, ArcadeNeon, 220f, false), "ui.cancel", "Отмена");
            var yes = Bind(ArcadePanelButton(w, "Yes", ResetSave, ArcadeRed, 260f, true, 18f, true), "ui.yes", "Да, сбросить");
            _confirmButtons.Add(no);
            _confirmButtons.Add(yes);
            _confirm.gameObject.SetActive(false);
        }

        public void OpenConfirm()
        {
            ClosePause();
            _confirm.gameObject.SetActive(true);
            _focused = FirstCandidate(_confirmButtons);
        }

        public void CloseConfirm()
        {
            _confirm.gameObject.SetActive(false);
            ClearFocus();
            OpenPause();
        }

        public void OpenUpgrade(CarInstance car)
        {
            var gm = GameManager.I;
            if (gm == null || car == null || _upgrade == null) return;
            var data = gm.Catalog != null ? gm.Catalog.SpecialFor(car.Data) : null;
            if (data == null) return;
            _upgradeCar = car;
            _holdProgress = 0f;
            _holdLatched = false;
            EnsurePreviewCamera();
            RebuildStage(null);
            _previewCam.gameObject.SetActive(true);
            _upgrade.gameObject.SetActive(true);
            _focused = FirstCandidate(_upgradeButtons);
            RefreshUpgrade();
        }

        public void CloseUpgrade()
        {
            if (_upgrade == null) return;
            _upgrade.gameObject.SetActive(false);
            if (_previewCam != null) _previewCam.gameObject.SetActive(false);
            if (_stageCar != null) { Destroy(_stageCar.gameObject); _stageCar = null; }
            _upgradeCar = null;
            _holdProgress = 0f;
            ClearFocus();
        }

        void EnsurePreviewCamera()
        {
            if (_previewCam != null) return;
            var stage = new GameObject("UpgradeStage");
            stage.transform.position = new Vector3(0f, -200f, 0f);
            _stage = stage.transform;
            var camGo = new GameObject("UpgradePreviewCamera");
            camGo.transform.SetParent(_stage, false);
            _previewCam = camGo.AddComponent<Camera>();
            _previewCam.clearFlags = CameraClearFlags.SolidColor;
            _previewCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _previewCam.cullingMask = 1 << PreviewLayer;
            _previewCam.fieldOfView = PreviewFov;
            _previewCam.nearClipPlane = 0.02f;
            _previewCam.farClipPlane = 20f;
            _previewCam.targetTexture = _previewRt;
            _previewCam.allowHDR = false;
            _previewCam.allowMSAA = false;
            var main = Camera.main;
            if (main != null) main.cullingMask &= ~(1 << PreviewLayer);
            camGo.SetActive(false);
        }

        void RebuildStage(List<GameObject> newParts)
        {
            var gm = GameManager.I;
            var data = gm != null && _upgradeCar != null ? gm.Catalog.SpecialFor(_upgradeCar.Data) : null;
            if (data == null) return;
            if (_stageCar != null) Destroy(_stageCar.gameObject);
            var holder = new GameObject("StageCar").transform;
            holder.SetParent(_stage, false);
            holder.localPosition = Vector3.zero;
            _stageCar = holder;
            var root = SpecialCarAssembly.Build(data, gm.SpecialLevel(_upgradeCar.Data), holder, PreviewLayer, newParts);
            root.localScale = Vector3.one;
            root.localPosition = Vector3.zero;
            var b = SpecialCarAssembly.LocalBounds(root);
            _stageCenter = b.center;
            _stageDistance = Mathf.Max(0.5f, b.size.magnitude * 0.52f / Mathf.Tan(PreviewFov * 0.5f * Mathf.Deg2Rad));
            ApplyStagePose(Vector3.zero);
        }

        void ApplyStagePose(Vector3 shake)
        {
            if (_stageCar == null || _previewCam == null) return;
            _stageCar.localRotation = Quaternion.Euler(0f, _stageYaw, 0f);
            _stageCar.localPosition = -(_stageCar.localRotation * _stageCenter) + shake;
            var camPos = Quaternion.Euler(PreviewElevation, 0f, 0f) * new Vector3(0f, 0f, -_stageDistance);
            _previewCam.transform.localPosition = camPos;
            _previewCam.transform.localRotation = Quaternion.LookRotation(-camPos.normalized, Vector3.up);
        }

        void UpdateUpgrade()
        {
            var gm = GameManager.I;
            if (_hoverText != null)
            {
                if (_interaction == null && gm != null && gm.Player != null) _interaction = gm.Player.GetComponent<PlayerInteraction>();
                var special = _interaction != null && !AnyOpen ? _interaction.HoverSpecial : null;
                string hover = "";
                if (special != null && gm.Catalog != null)
                {
                    var d = gm.Catalog.SpecialFor(special.Data);
                    if (d != null) hover = Loc.Get(special.Data.DisplayName, special.Data.DevName) + "\n" + string.Format(Loc.Get("ui.special_level", "Улучшений: {0}/{1}"), gm.SpecialLevel(special.Data), d.MaxLevel);
                }
                if (_hoverText.text != hover) _hoverText.text = hover;
            }
            if (!UpgradeOpen || gm == null || _upgradeCar == null) return;

            var drag = _upgradeDrag != null ? _upgradeDrag.Consume() : Vector2.zero;
            if (drag.x != 0f) _stageYaw += drag.x * DragDegreesPerPixel;
            if (_focused == _holdButton)
            {
                var nav = GameInput.Ui.Navigate.VectorValue;
                if (Mathf.Abs(nav.x) > 0.3f) _stageYaw += nav.x * 90f * Time.unscaledDeltaTime;
            }

            bool can = gm.CanUpgradeSpecial(_upgradeCar);
            bool held = _holdButton.Held || (GameInput.Ui.Submit.Held() && _focused == _holdButton);
            if (!held) _holdLatched = false;
            held &= !_holdLatched;
            if (_holdButton.PressedThisFrame)
            {
                _holdButton.ClearPressed();
                if (!can) Messages.Show(gm.Crates <= 0 ? Loc.Get("msg.no_crates", "Нет ящиков запчастей") : Loc.Get("msg.special_maxed", "Машина прокачана полностью"));
            }
            float holdTime = Mathf.Max(0.2f, gm.Config.SpecialHoldTime);
            if (held && can) _holdProgress = Mathf.Min(1f, _holdProgress + Time.unscaledDeltaTime / holdTime);
            else _holdProgress = Mathf.Max(0f, _holdProgress - Time.unscaledDeltaTime / (holdTime * 0.5f));
            _holdFill.fillAmount = _holdProgress;

            float amp = gm.Config.SpecialShake * _holdProgress * _holdProgress;
            var shake = amp > 0f ? new Vector3(Mathf.Sin(Time.unscaledTime * 47f), Mathf.Sin(Time.unscaledTime * 61f + 1f), Mathf.Sin(Time.unscaledTime * 53f + 2f)) * amp * _stageDistance : Vector3.zero;
            ApplyStagePose(shake);

            if (_holdProgress >= 1f)
            {
                _holdProgress = 0f;
                _holdFill.fillAmount = 0f;
                _holdLatched = true;
                var podiumParts = new List<GameObject>();
                if (gm.UpgradeSpecial(_upgradeCar, podiumParts))
                {
                    var previewParts = new List<GameObject>();
                    RebuildStage(previewParts);
                    StartCoroutine(Reveal(previewParts, podiumParts));
                    Sfx.PlayUi(gm.Config.SpecialUpgradeClip);
                    Rumble.ShelfComplete();
                    RefreshUpgrade();
                }
            }
        }

        IEnumerator Reveal(List<GameObject> previewParts, List<GameObject> podiumParts)
        {
            var gm = GameManager.I;
            float duration = gm != null ? gm.Config.SpecialRevealDuration : 0.45f;
            float glow = gm != null ? gm.Config.SpecialGlowDuration : 1.5f;
            var all = new List<GameObject>(previewParts);
            all.AddRange(podiumParts);
            var outlines = new List<GameObject>();
            foreach (var part in all)
            {
                if (part == null) continue;
                var mf = part.GetComponent<MeshFilter>();
                if (mf == null) continue;
                var o = new GameObject("Glow");
                o.layer = part.layer;
                o.transform.SetParent(part.transform, false);
                o.AddComponent<MeshFilter>().sharedMesh = MeshGhost.Smoothed(mf.sharedMesh);
                var r = o.AddComponent<MeshRenderer>();
                r.sharedMaterial = GoldOutline();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlines.Add(o);
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float s = k < 0.6f ? Mathf.Lerp(0.2f, 1.3f, k / 0.6f) : Mathf.Lerp(1.3f, 1f, (k - 0.6f) / 0.4f);
                foreach (var part in all) if (part != null) part.transform.localScale = Vector3.one * s;
                yield return null;
            }
            foreach (var part in all) if (part != null) part.transform.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, glow - duration));
            foreach (var o in outlines) if (o != null) Destroy(o);
        }

        Material GoldOutline()
        {
            if (_goldOutline == null)
            {
                _goldOutline = new Material(Shader.Find("SortThem/Outline"));
                _goldOutline.SetColor("_Color", Gold);
                _goldOutline.SetFloat("_Width", 7f);
            }
            return _goldOutline;
        }

        void RefreshUpgrade()
        {
            var gm = GameManager.I;
            if (!UpgradeOpen || gm == null || _upgradeCar == null) return;
            var data = gm.Catalog.SpecialFor(_upgradeCar.Data);
            if (data == null) return;
            int level = gm.SpecialLevel(_upgradeCar.Data);
            _upgradeTitle.text = Loc.Get(_upgradeCar.Data.DisplayName, _upgradeCar.Data.DevName);
            _upgradeLevel.text = string.Format(Loc.Get("ui.special_level", "Улучшений: {0}/{1}"), level, data.MaxLevel);
            bool maxed = level >= data.MaxLevel;
            _upgradeNext.text = maxed
                ? Loc.Get("ui.special_done", "Все детали установлены")
                : string.Format(Loc.Get("ui.special_next", "Следующая деталь: {0}"), Loc.Get(data.Steps[level].DisplayName, data.Steps[level].DevName));
            _upgradeCrates.text = Loc.Get("ui.crates", "Ящики запчастей") + ": " + gm.Crates;
            _holdLabel.text = maxed ? Loc.Get("ui.special_max", "Максимум") : Loc.Get("ui.special_hold", "Удерживай, чтобы улучшить");
            _holdButton.interactable = !maxed;
        }
    }
}

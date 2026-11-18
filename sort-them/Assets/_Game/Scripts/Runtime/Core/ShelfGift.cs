using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SortThem
{
    public class ShelfGift : MonoBehaviour
    {
        const float Size = 0.105f;
        const float SpinSpeed = 80f;
        const float BobHeight = 0.01f;
        const float SideInset = 0.2f;
        const float LabelTurn = 30f;
        const float BobHz = 0.8f;
        const float CullDistance = 18f;
        const float LabelDistance = 3.5f;
        const float Interval = 0.2f;
        const float WaitTint = 0.55f;

        static readonly Color BodyReady = new Color(0.9f, 0.13f, 0.16f);
        static readonly Color RibbonReady = new Color(1f, 0.8f, 0.22f);
        static readonly Color BodyWait = new Color(0.5f, 0.5f, 0.53f);
        static readonly Color RibbonWait = new Color(0.7f, 0.7f, 0.73f);
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        const float LabelWidth = 0.35f;
        const float LabelHeight = 0.056f;
        const float LabelRadius = 0.02f;

        static readonly Color TextReady = new Color(0.25f, 0.18f, 0f);
        static readonly Color TextWait = new Color(0.27f, 0.27f, 0.3f);

        static Material _body, _ribbon, _spark, _plateReady, _plateWait;
        static Mesh _cube, _pill;
        static int _tintFrame = -1;

        public ShelfController Shelf;

        Transform _visual, _spin, _sparks, _labelRoot;
        TextMeshPro _label;
        Renderer _labelBack;
        Collider _collider;
        Camera _cam;
        bool _shown;
        float _next;
        float _phase;

        public MeshFilter MainMesh { get; private set; }
        public bool Shown => _shown;

        public static ShelfGift Attach(ShelfController shelf)
        {
            if (shelf == null || shelf.Tag == null || shelf.Tag.RedMaterial == null) return null;
            EnsureShared(shelf.Tag);
            var tag = shelf.Tag;
            float half = tag.Root != null ? tag.Root.transform.localScale.x * 0.5f : 0.25f;
            if (shelf.Zone is BoxCollider zone) half = Mathf.Max(half, zone.size.x * 0.5f);
            var root = new GameObject("ShelfGift");
            root.transform.SetPositionAndRotation(tag.transform.TransformPoint(new Vector3(-Mathf.Max(0f, half - SideInset), 0.1f, 0.09f)), tag.transform.rotation);
            root.transform.SetParent(shelf.transform, true);
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * (Size * 1.8f);
            box.enabled = false;
            var gift = root.AddComponent<ShelfGift>();
            gift.Shelf = shelf;
            gift._collider = box;
            gift._phase = shelf.ShelfId * 0.37f;
            return gift;
        }

        static void EnsureShared(PriceTag tag)
        {
            if (_body != null) return;
            _body = new Material(tag.RedMaterial) { name = "GiftBody" };
            _ribbon = new Material(tag.RedMaterial) { name = "GiftRibbon" };
            _spark = new Material(tag.RedMaterial) { name = "GiftSpark" };
            _plateReady = new Material(tag.RedMaterial) { name = "GiftLabelReady" };
            _plateWait = new Material(tag.RedMaterial) { name = "GiftLabelWait" };
            _spark.SetColor(BaseColor, new Color(1f, 0.92f, 0.45f));
            _plateReady.SetColor(BaseColor, new Color(1f, 0.8f, 0.24f));
            _plateWait.SetColor(BaseColor, new Color(0.86f, 0.86f, 0.88f));
            _pill = RoundedPlate(LabelWidth, LabelHeight, LabelRadius);
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cube = probe.GetComponent<MeshFilter>().sharedMesh;
            Destroy(probe);
        }

        static Mesh RoundedPlate(float width, float height, float radius)
        {
            const int steps = 6;
            var verts = new List<Vector3> { Vector3.zero };
            float cx = width * 0.5f - radius, cy = height * 0.5f - radius;
            var centers = new[] { new Vector2(-cx, cy), new Vector2(cx, cy), new Vector2(cx, -cy), new Vector2(-cx, -cy) };
            for (int c = 0; c < 4; c++)
            {
                float from = 180f - c * 90f;
                for (int i = 0; i <= steps; i++)
                {
                    float a = (from - 90f * i / steps) * Mathf.Deg2Rad;
                    verts.Add(new Vector3(centers[c].x + Mathf.Cos(a) * radius, centers[c].y + Mathf.Sin(a) * radius, 0f));
                }
            }
            int n = verts.Count - 1;
            var tris = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int a = 1 + i, b = 1 + (i + 1) % n;
                tris.Add(0); tris.Add(a); tris.Add(b);
                tris.Add(0); tris.Add(b); tris.Add(a);
            }
            var mesh = new Mesh { name = "GiftLabelPlate" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static bool Available => Premium.Owned || Ads.Ready;

        static void Tint()
        {
            if (_tintFrame == Time.frameCount || _body == null) return;
            _tintFrame = Time.frameCount;
            float k = Available ? 1f : Ads.Progress * WaitTint;
            _body.SetColor(BaseColor, Color.Lerp(BodyWait, BodyReady, k));
            _ribbon.SetColor(BaseColor, Color.Lerp(RibbonWait, RibbonReady, k));
        }

        void Build()
        {
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            _spin = new GameObject("Spin").transform;
            _spin.SetParent(_visual, false);
            MainMesh = Block(_spin, "Box", Vector3.zero, Vector3.zero, Vector3.one * Size, _body);
            Block(_spin, "RibbonX", Vector3.zero, Vector3.zero, new Vector3(Size * 1.04f, Size * 1.04f, Size * 0.22f), _ribbon);
            Block(_spin, "RibbonZ", Vector3.zero, Vector3.zero, new Vector3(Size * 0.22f, Size * 1.04f, Size * 1.04f), _ribbon);
            Block(_spin, "BowLeft", new Vector3(-Size * 0.2f, Size * 0.6f, 0f), new Vector3(0f, 0f, 30f), new Vector3(Size * 0.42f, Size * 0.2f, Size * 0.2f), _ribbon);
            Block(_spin, "BowRight", new Vector3(Size * 0.2f, Size * 0.6f, 0f), new Vector3(0f, 0f, -30f), new Vector3(Size * 0.42f, Size * 0.2f, Size * 0.2f), _ribbon);

            _sparks = new GameObject("Sparks").transform;
            _sparks.SetParent(_visual, false);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                var pos = new Vector3(Mathf.Cos(a) * Size * 1.05f, (i % 2 == 0 ? 0.35f : -0.2f) * Size, Mathf.Sin(a) * Size * 1.05f);
                Block(_sparks, "Spark" + i, pos, new Vector3(45f, 45f, 0f), Vector3.one * (Size * 0.14f), _spark);
            }

            _labelRoot = new GameObject("Label").transform;
            _labelRoot.SetParent(_visual, false);
            _labelRoot.localPosition = new Vector3(0f, Size * 0.5f + 0.085f, 0f);
            var back = Block(_labelRoot, "Back", new Vector3(0f, 0f, 0.004f), Vector3.zero, Vector3.one, _plateWait);
            back.sharedMesh = _pill;
            _labelBack = back.GetComponent<Renderer>();
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_labelRoot, false);
            _label = textGo.AddComponent<TextMeshPro>();
            if (Shelf.Tag.NameText != null) _label.font = Shelf.Tag.NameText.font;
            Fonts.Register(_label);
            _label.color = TextWait;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.enableAutoSizing = true;
            _label.fontSizeMin = 0.05f;
            _label.fontSizeMax = 0.42f;
            _label.rectTransform.sizeDelta = new Vector2(LabelWidth - 0.04f, LabelHeight - 0.014f);
            Quiet(_label.GetComponent<Renderer>());
            _labelRoot.gameObject.SetActive(false);
        }

        static MeshFilter Block(Transform parent, string name, Vector3 position, Vector3 euler, Vector3 scale, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = _cube;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            Quiet(renderer);
            return filter;
        }

        static void Quiet(Renderer renderer)
        {
            if (renderer == null) return;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        void Update()
        {
            if (Time.unscaledTime >= _next)
            {
                _next = Time.unscaledTime + Interval;
                Refresh();
            }
            if (!_shown) return;
            Tint();
            float t = Time.time + _phase;
            _spin.localRotation = Quaternion.Euler(0f, t * SpinSpeed, 0f);
            _spin.localPosition = new Vector3(0f, Mathf.Sin(t * BobHz * 2f * Mathf.PI) * BobHeight, 0f);
            if (_sparks.gameObject.activeSelf)
            {
                _sparks.localRotation = Quaternion.Euler(0f, -t * SpinSpeed * 2.2f, 0f);
                _sparks.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t * 9f));
            }
            if (_labelRoot.gameObject.activeSelf && _cam != null)
            {
                var away = _labelRoot.position - _cam.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                {
                    var front = -transform.forward;
                    float turn = Mathf.Clamp(Vector3.SignedAngle(front, away, Vector3.up), -LabelTurn, LabelTurn);
                    _labelRoot.rotation = Quaternion.LookRotation(Quaternion.AngleAxis(turn, Vector3.up) * front);
                }
            }
        }

        void Refresh()
        {
            var gm = GameManager.I;
            if (_cam == null) _cam = Camera.main;
            bool on = gm != null && gm.Ready && Shelf != null && Shelf.IsClosed && (Ads.RewardedAvailable || Premium.Owned) && !gm.GiftClaimed.Contains(Shelf.ShelfId);
            float distance = _cam != null ? Vector3.Distance(_cam.transform.position, transform.position) : 0f;
            if (on && distance > CullDistance) on = false;
            if (on && _visual == null) Build();
            if (on != _shown)
            {
                _shown = on;
                if (_visual != null) _visual.gameObject.SetActive(on);
                _collider.enabled = on;
            }
            if (!on) return;
            bool ready = Available;
            if (_sparks.gameObject.activeSelf != ready) _sparks.gameObject.SetActive(ready);
            bool near = distance <= LabelDistance;
            if (_labelRoot.gameObject.activeSelf != near) _labelRoot.gameObject.SetActive(near);
            if (!near) return;
            string text = Premium.Owned ? "+$" + Reward(gm)
                : ready ? string.Format(Loc.Get("ui.ad_spin", "За рекламу · ${0}"), Reward(gm))
                : string.Format(Loc.Get("ui.ad_wait", "Реклама через {0}"), Ads.RemainingText());
            if (_label.text != text) _label.text = text;
            var plate = ready ? _plateReady : _plateWait;
            if (_labelBack.sharedMaterial != plate)
            {
                _labelBack.sharedMaterial = plate;
                _label.color = ready ? TextReady : TextWait;
            }
        }

        static int Reward(GameManager gm) => Mathf.RoundToInt(gm.Config.ShelfGiftReward);

        public void Claim()
        {
            var gm = GameManager.I;
            if (gm == null || !_shown || Shelf == null) return;
            if (Premium.Owned)
            {
                Grant(gm);
                return;
            }
            if (!Ads.Ready)
            {
                Messages.Show(string.Format(Loc.Get("ui.ad_wait", "Реклама через {0}"), Ads.RemainingText()));
                return;
            }
            Ads.ShowRewarded(ok => { if (ok && this != null) Grant(gm); });
        }

        void Grant(GameManager gm)
        {
            int id = Shelf.ShelfId;
            if (gm.GiftClaimed.Contains(id)) return;
            int reward = Reward(gm);
            gm.GiftClaimed.Add(id);
            gm.Economy.Add(reward);
            Sfx.PlayUi(gm.Config.PurchaseClip);
            Messages.Show("+$" + reward);
            gm.Save.SaveNow("gift");
            Refresh();
        }
    }

#if UNITY_WEBGL
    public class ShelfGiftSpawner : MonoBehaviour
    {
        readonly Dictionary<int, ShelfGift> _gifts = new Dictionary<int, ShelfGift>();
        GameManager _gm;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (s, m) => Spawn();
            Spawn();
        }

        static void Spawn()
        {
            if (FindFirstObjectByType<ShelfGiftSpawner>() != null) return;
            if (FindFirstObjectByType<ShelfController>() == null) return;
            new GameObject("[ShelfGifts]").AddComponent<ShelfGiftSpawner>();
        }

        IEnumerator Start()
        {
            while (GameManager.I == null || !GameManager.I.Ready) yield return null;
            _gm = GameManager.I;
            _gm.ShelfClosed += Add;
            foreach (var shelf in _gm.Shelves) if (shelf.IsClosed) Add(shelf);
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.ShelfClosed -= Add;
        }

        void Add(ShelfController shelf)
        {
            if (shelf == null || _gifts.ContainsKey(shelf.ShelfId) || _gm.GiftClaimed.Contains(shelf.ShelfId)) return;
            var gift = ShelfGift.Attach(shelf);
            if (gift != null) _gifts[shelf.ShelfId] = gift;
        }
    }
#endif
}

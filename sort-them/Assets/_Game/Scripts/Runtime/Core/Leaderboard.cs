using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SortThem
{
    public struct LeaderboardEntry
    {
        public int Rank;
        public string Name;
        public int Score;
        public bool Me;
    }

    public static class Leaderboard
    {
        public const string Id = "cars_sorted";
        const string KeyBest = "lb.best";
        const float SubmitInterval = 20f;
        const float RetryInterval = 120f;
        const float FetchInterval = 180f;
        const float FetchAfterSubmit = 2f;
        const int MaxFailures = 3;

        public static Func<bool> CanSubmit;
        public static Func<bool> CanFetch;
        public static Func<string> PlayerName;
        public static Action<int, Action<bool>> Submit;
        public static Action<Action<List<LeaderboardEntry>>> Fetch;

        public static readonly List<LeaderboardEntry> Entries = new List<LeaderboardEntry>();
        public static event Action Changed;

        static int _best = -1;
        static bool _dirty, _busy;
        static int _failures;
        static float _nextSubmit, _nextFetch;

        public static bool Loaded { get; private set; }

        public static int Best
        {
            get
            {
                if (_best >= 0) return _best;
                var prefs = SavesReady.Prefs;
                if (prefs == null) return 0;
                _best = Mathf.Max(0, prefs.GetInt(KeyBest, 0));
                return _best;
            }
        }

        public static void Begin(int placed)
        {
            _best = -1;
            _busy = false;
            _failures = 0;
            _nextSubmit = 0f;
            _nextFetch = 0f;
            Track(placed);
            _dirty = Best > 0;
        }

        public static void Track(int placed)
        {
            var prefs = SavesReady.Prefs;
            if (prefs == null || placed <= Best) return;
            _best = placed;
            prefs.SetInt(KeyBest, placed);
            _dirty = true;
            Changed?.Invoke();
        }

        public static void Tick()
        {
            float now = Time.unscaledTime;
            if (_dirty && !_busy && now >= _nextSubmit && _failures < MaxFailures && Submit != null && CanSubmit != null && CanSubmit())
            {
                _dirty = false;
                _busy = true;
                _nextSubmit = now + SubmitInterval;
                int sent = _best;
                var prefs = SavesReady.Prefs;
                if (prefs != null) prefs.TrySave();
                Submit(sent, ok =>
                {
                    _busy = false;
                    Debug.Log("SortThem: leaderboard score " + sent + " sent, ok = " + ok);
                    if (ok)
                    {
                        _failures = 0;
                        _nextFetch = Mathf.Min(_nextFetch, Time.unscaledTime + FetchAfterSubmit);
                        return;
                    }
                    _failures++;
                    _dirty = true;
                    _nextSubmit = Time.unscaledTime + RetryInterval;
                });
            }
            if (now >= _nextFetch && Fetch != null && CanFetch != null && CanFetch())
            {
                _nextFetch = now + FetchInterval;
                Fetch(list =>
                {
                    Debug.Log("SortThem: leaderboard entries " + (list != null ? list.Count.ToString() : "failed"));
                    if (list == null) return;
                    Entries.Clear();
                    Entries.AddRange(list);
                    Loaded = true;
                    Changed?.Invoke();
                });
            }
        }
    }

    public class LeaderboardBoard : MonoBehaviour
    {
        const int Rows = 10;
        const int NameLimit = 18;
        const float Width = 1.3f;
        const float PanelHeight = 1.5f;
        const float PanelBottom = 0.75f;
        const float Depth = 0.04f;
        const float Frame = 0.045f;
        const float Pad = 0.05f;
        const float HeaderHeight = 0.2f;
        const float RowHeight = 0.1f;
        const float OwnHeight = 0.13f;
        const float TextZ = -(Depth * 0.5f + 0.008f);
        const float CullDistance = 14f;
        const float Interval = 0.25f;

        static readonly Vector3 Position = new Vector3(9.02f, 0f, -4.5f);
        static readonly Color PanelColor = new Color(0.09f, 0.12f, 0.22f);
        static readonly Color StripeColor = new Color(0.13f, 0.17f, 0.3f);
        static readonly Color GoldColor = new Color(1f, 0.8f, 0.24f);
        static readonly Color WoodColor = new Color(0.36f, 0.23f, 0.13f);
        static readonly Color OwnText = new Color(0.25f, 0.18f, 0f);
        static readonly string[] RankColors = { "#FFD24A", "#D5DBE6", "#E0965A" };

        readonly TextMeshPro[] _names = new TextMeshPro[Rows];
        readonly TextMeshPro[] _scores = new TextMeshPro[Rows];
        readonly StringBuilder _sb = new StringBuilder();
        TextMeshPro _title, _ownName, _ownScore;
        GameObject _content;
        Material _wood;
        Camera _cam;
        float _next;

        public static LeaderboardBoard Build()
        {
            var tag = FindFirstObjectByType<PriceTag>(FindObjectsInactive.Include);
            if (tag == null || tag.RedMaterial == null) return null;
            var root = new GameObject("LeaderboardBoard");
            root.transform.position = Position;
            var board = root.AddComponent<LeaderboardBoard>();
            board.Construct(tag);
            return board;
        }

        void Construct(PriceTag tag)
        {
            var panel = Tinted(tag, PanelColor);
            var stripe = Tinted(tag, StripeColor);
            var gold = Tinted(tag, GoldColor);
            var wood = SceneWood();
            if (wood != null) _wood = wood;
            else wood = Tinted(tag, WoodColor);
            var font = tag.NameText != null ? tag.NameText.font : null;

            float top = PanelBottom + PanelHeight;
            float mid = PanelBottom + PanelHeight * 0.5f;
            float legX = Width * 0.5f - 0.14f;
            float legHeight = PanelBottom + 0.12f;
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, top * 0.5f, 0.02f);
            box.size = new Vector3(Width + Frame * 2f, top, 0.16f);

            Block("Panel", new Vector3(0f, mid, 0f), new Vector3(Width, PanelHeight, Depth), panel);
            Block("FrameTop", new Vector3(0f, top + Frame * 0.5f, 0f), new Vector3(Width + Frame * 2f, Frame, Depth + 0.02f), wood);
            Block("FrameBottom", new Vector3(0f, PanelBottom - Frame * 0.5f, 0f), new Vector3(Width + Frame * 2f, Frame, Depth + 0.02f), wood);
            Block("FrameLeft", new Vector3(-(Width + Frame) * 0.5f, mid, 0f), new Vector3(Frame, PanelHeight, Depth + 0.02f), wood);
            Block("FrameRight", new Vector3((Width + Frame) * 0.5f, mid, 0f), new Vector3(Frame, PanelHeight, Depth + 0.02f), wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Block("Leg", new Vector3(side * legX, legHeight * 0.5f, 0.05f), new Vector3(0.06f, legHeight, 0.06f), wood);
                Block("Foot", new Vector3(side * legX, 0.02f, 0.05f), new Vector3(0.08f, 0.04f, 0.5f), wood);
            }

            _content = new GameObject("Content");
            _content.transform.SetParent(transform, false);
            float inner = Width - Pad * 2f;
            float left = -Width * 0.5f + Pad;
            float scoreWidth = 0.3f;
            float nameWidth = inner - scoreWidth - 0.06f;
            float nameX = left + 0.03f + nameWidth * 0.5f;
            float scoreX = Width * 0.5f - Pad - 0.03f - scoreWidth * 0.5f;

            float y = top - Pad - HeaderHeight * 0.5f;
            _title = Text(font, "Title", 0f, y, inner, HeaderHeight * 0.8f, 1.2f, TextAlignmentOptions.Center, GoldColor);
            Block("Rule", new Vector3(0f, top - Pad - HeaderHeight, -Depth * 0.5f), new Vector3(inner, 0.008f, 0.004f), gold);

            float rowsTop = top - Pad - HeaderHeight - 0.03f;
            for (int i = 0; i < Rows; i++)
            {
                y = rowsTop - RowHeight * (i + 0.5f);
                if (i % 2 == 0) Block("Stripe" + i, new Vector3(0f, y, -Depth * 0.5f), new Vector3(inner, RowHeight, 0.004f), stripe);
                _names[i] = Text(font, "Name" + i, nameX, y, nameWidth, RowHeight * 0.8f, 0.6f, TextAlignmentOptions.Left, Color.white);
                _scores[i] = Text(font, "Score" + i, scoreX, y, scoreWidth, RowHeight * 0.8f, 0.6f, TextAlignmentOptions.Right, Color.white);
            }

            y = rowsTop - RowHeight * Rows - 0.04f - OwnHeight * 0.5f;
            Block("Own", new Vector3(0f, y, -Depth * 0.5f), new Vector3(inner, OwnHeight, 0.006f), gold);
            _ownName = Text(font, "OwnName", nameX, y, nameWidth, OwnHeight * 0.8f, 0.7f, TextAlignmentOptions.Left, OwnText);
            _ownScore = Text(font, "OwnScore", scoreX, y, scoreWidth, OwnHeight * 0.8f, 0.7f, TextAlignmentOptions.Right, OwnText);
        }

        static Material SceneWood()
        {
            var doors = GameObject.Find("Doors");
            var jamb = doors != null ? doors.transform.Find("JambL") : null;
            var renderer = jamb != null ? jamb.GetComponentInChildren<Renderer>() : null;
            return renderer != null ? renderer.sharedMaterial : null;
        }

        static Material Tinted(PriceTag tag, Color color)
        {
            var material = new Material(tag.RedMaterial);
            material.SetColor("_BaseColor", color);
            return material;
        }

        void Block(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            if (material == _wood)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            else Quiet(renderer);
        }

        TextMeshPro Text(TMP_FontAsset font, string name, float x, float y, float width, float height, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_content.transform, false);
            go.transform.localPosition = new Vector3(x, y, TextZ);
            var text = go.AddComponent<TextMeshPro>();
            if (font != null) text.font = font;
            Fonts.Register(text);
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.5f;
            text.fontSizeMax = size;
            text.rectTransform.sizeDelta = new Vector2(width, height);
            Quiet(text.GetComponent<Renderer>());
            return text;
        }

        static void Quiet(Renderer renderer)
        {
            if (renderer == null) return;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        void OnEnable()
        {
            Leaderboard.Changed += Refresh;
            Loc.Changed += Refresh;
        }

        void OnDisable()
        {
            Leaderboard.Changed -= Refresh;
            Loc.Changed -= Refresh;
        }

        void Start() => Refresh();

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + Interval;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            var to = transform.position - _cam.transform.position;
            bool on = to.sqrMagnitude < CullDistance * CullDistance && to.z > 0f;
            if (_content.activeSelf != on) _content.SetActive(on);
        }

        void Refresh()
        {
            if (_title == null) return;
            _title.text = Loc.Get("ui.cars", "Cars");
            var entries = Leaderboard.Entries;
            int me = -1;
            for (int i = 0; i < Rows; i++)
            {
                if (i >= entries.Count)
                {
                    _names[i].text = Rank(i + 1, "");
                    _scores[i].text = "";
                    continue;
                }
                var e = entries[i];
                if (e.Me) me = i;
                _names[i].text = Rank(e.Rank > 0 ? e.Rank : i + 1, Clean(e.Name, _names[i].font));
                _scores[i].text = e.Score.ToString();
                var color = e.Me ? GoldColor : Color.white;
                _names[i].color = color;
                _scores[i].color = color;
            }
            for (int i = Rows; i < entries.Count && me < 0; i++) if (entries[i].Me) me = i;

            int best = Leaderboard.Best;
            if (me >= 0)
            {
                var e = entries[me];
                _ownName.text = (e.Rank > 0 ? e.Rank : me + 1) + "<pos=16%>" + Clean(e.Name, _ownName.font);
                _ownScore.text = Mathf.Max(e.Score, best).ToString();
            }
            else
            {
                string name = Leaderboard.PlayerName != null ? Leaderboard.PlayerName() : null;
                _ownName.text = "-<pos=16%>" + Clean(name, _ownName.font);
                _ownScore.text = best.ToString();
            }
        }

        static string Rank(int rank, string name)
        {
            string color = rank >= 1 && rank <= RankColors.Length ? RankColors[rank - 1] : "#8D97B5";
            return "<color=" + color + ">" + rank + "</color><pos=16%>" + name;
        }

        string Clean(string name, TMP_FontAsset font)
        {
            _sb.Clear();
            if (name != null)
            {
                foreach (char c in name)
                {
                    if (c == '<' || c == '>' || char.IsControl(c) || char.IsSurrogate(c)) continue;
                    if (font != null && !font.HasCharacter(c, true)) continue;
                    _sb.Append(c);
                    if (_sb.Length >= NameLimit) break;
                }
            }
            string clean = _sb.ToString().Trim();
            return clean.Length > 0 ? clean : "???";
        }
    }

#if UNITY_WEBGL
    public class LeaderboardTracker : MonoBehaviour
    {
        GameManager _gm;
        LeaderboardBoard _board;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (s, m) => Spawn();
            Spawn();
        }

        static void Spawn()
        {
            if (FindFirstObjectByType<LeaderboardTracker>() != null) return;
            if (FindFirstObjectByType<ShelfController>() == null) return;
            new GameObject("[Leaderboard]").AddComponent<LeaderboardTracker>();
        }

        IEnumerator Start()
        {
            while (GameManager.I == null || !GameManager.I.Ready) yield return null;
            _gm = GameManager.I;
            Leaderboard.Begin(_gm.PlacedValid);
            _gm.StatsChanged += OnStats;
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.StatsChanged -= OnStats;
        }

        void OnStats() => Leaderboard.Track(_gm.PlacedValid);

        void Update()
        {
            if (_gm == null) return;
            Leaderboard.Tick();
            if (_board == null && Leaderboard.Loaded) _board = LeaderboardBoard.Build();
        }
    }
#endif
}

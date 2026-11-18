using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SortThem.Web
{
    public class WebSaleSign : MonoBehaviour
    {
        const float PlateWidth = 0.56f;
        const float PlateHeight = 0.32f;
        const float PlateDepth = 0.02f;
        const float PoleHeight = 0.26f;
        const float PoleWidth = 0.035f;
        const float SwingDegrees = 9f;
        const float SwingHz = 1.1f;
        const float PopTime = 0.3f;
        const float Interval = 0.25f;

        static WebSaleSign _terminalSign, _slotSign;

        Transform _pivot;
        TextMeshPro _text;
        Camera _cam;
        bool _free;
        bool _visible;
        float _shown;
        float _next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded += (s, m) => Attach();
            Attach();
        }

        static void Attach()
        {
            var tag = FindFirstObjectByType<PriceTag>(FindObjectsInactive.Include);
            if (tag == null || tag.RedMaterial == null) return;
            if (_terminalSign == null)
            {
                var terminal = FindFirstObjectByType<UpgradeTerminal>();
                if (terminal != null) _terminalSign = Build(terminal, tag, false);
            }
            if (_slotSign == null)
            {
                var slot = FindFirstObjectByType<SlotMachine>();
                if (slot != null) _slotSign = Build(slot, tag, true);
            }
        }

        static WebSaleSign Build(Component owner, PriceTag tag, bool free)
        {
            if (!TryBounds(owner, out var bounds)) return null;
            var root = new GameObject("SaleSign_" + owner.name);
            root.transform.position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            var sign = root.AddComponent<WebSaleSign>();
            sign._free = free;

            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root.transform, false);
            sign._pivot = pivot;

            var pole = tag.WhiteMaterial != null ? tag.WhiteMaterial : tag.RedMaterial;
            Block(pivot, "Pole", new Vector3(0f, PoleHeight * 0.5f, 0f), new Vector3(PoleWidth, PoleHeight, PoleWidth), pole);
            float plateY = PoleHeight + PlateHeight * 0.5f;
            Block(pivot, "Plate", new Vector3(0f, plateY, 0f), new Vector3(PlateWidth, PlateHeight, PlateDepth), tag.RedMaterial);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(pivot, false);
            textGo.transform.localPosition = new Vector3(0f, plateY, -(PlateDepth * 0.5f + 0.005f));
            var text = textGo.AddComponent<TextMeshPro>();
            if (tag.NameText != null) text.font = tag.NameText.font;
            Fonts.Register(text);
            text.text = "%";
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = 0.1f;
            text.fontSizeMax = 4f;
            text.rectTransform.sizeDelta = new Vector2(PlateWidth * 0.9f, PlateHeight * 0.8f);
            Quiet(text.GetComponent<Renderer>());
            sign._text = text;

            pivot.gameObject.SetActive(false);
            return sign;
        }

        static void Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            Quiet(renderer);
        }

        static void Quiet(Renderer renderer)
        {
            if (renderer == null) return;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        static bool TryBounds(Component owner, out Bounds bounds)
        {
            bounds = default;
            var collider = owner.GetComponent<Collider>();
            if (collider != null) { bounds = collider.bounds; return true; }
            bool any = false;
            foreach (var r in owner.GetComponentsInChildren<Renderer>())
            {
                if (any) bounds.Encapsulate(r.bounds);
                else { bounds = r.bounds; any = true; }
            }
            return any;
        }

        void Update()
        {
            if (Time.unscaledTime >= _next)
            {
                _next = Time.unscaledTime + Interval;
                var gm = GameManager.I;
                _visible = gm != null && gm.Ready && Ads.RewardedAvailable && Ads.Ready;
                if (_visible)
                {
                    string label = "-" + (_free ? 100 : Ads.DiscountPercent) + "%";
                    if (_text.text != label) _text.text = label;
                }
            }
            _shown = Mathf.MoveTowards(_shown, _visible ? 1f : 0f, Time.deltaTime / PopTime);
            bool on = _shown > 0f;
            if (_pivot.gameObject.activeSelf != on) _pivot.gameObject.SetActive(on);
            if (!on) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam != null)
            {
                var away = transform.position - _cam.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(away);
            }
            float k = _shown - 1f;
            float pop = 1f + 2.70158f * k * k * k + 1.70158f * k * k;
            _pivot.localScale = Vector3.one * Mathf.Max(0f, pop);
            _pivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * SwingHz * 2f * Mathf.PI) * SwingDegrees);
        }
    }
}

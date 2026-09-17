using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SortThem
{
    public class UiCrtPower : MonoBehaviour
    {
        public float Duration = 0.24f;
        public float FlashAlpha = 0.7f;

        Image _flash;

        void Awake()
        {
            var rt = UiFactory.Rect(transform, "Flash");
            UiFactory.Anchor(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _flash = rt.gameObject.AddComponent<Image>();
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _flash.raycastTarget = false;
        }

        void OnEnable()
        {
            _flash.transform.SetAsLastSibling();
            StartCoroutine(Run());
        }

        void OnDisable()
        {
            transform.localScale = Vector3.one;
            _flash.color = new Color(1f, 1f, 1f, 0f);
        }

        IEnumerator Run()
        {
            float t = 0f;
            while (t < Duration * 1.6f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / Duration);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                float y = Mathf.Lerp(0.015f, 1f, e);
                float x = Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(k * 2f));
                transform.localScale = new Vector3(x, y, 1f);
                float f = Mathf.Clamp01(t / (Duration * 1.6f));
                _flash.color = new Color(1f, 1f, 1f, FlashAlpha * (1f - f) * (1f - f));
                yield return null;
            }
            transform.localScale = Vector3.one;
            _flash.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}

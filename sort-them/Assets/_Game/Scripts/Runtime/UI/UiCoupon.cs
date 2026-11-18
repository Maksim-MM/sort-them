using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SortThem
{
    public class UiCoupon : MonoBehaviour
    {
        public RectTransform Card;
        public CanvasGroup Group;
        public RectTransform Glint;

        const float BaseRoll = -5f;
        const float PopTime = 0.3f;
        const float SpendTime = 0.55f;
        const float SpendRise = 170f;
        const float GlintPeriod = 2.6f;
        const float GlintSweep = 0.8f;

        bool _shown, _spending;
        float _pop = 1f, _t;
        Vector2 _hover;

        public void Set(bool on)
        {
            if (on == _shown) return;
            _shown = on;
            if (Card == null) return;
            if (on)
            {
                StopAllCoroutines();
                ResetCard(true);
                _pop = 0f;
            }
            else if (isActiveAndEnabled && Card.gameObject.activeSelf) StartCoroutine(Spend());
            else ResetCard(false);
        }

        void OnEnable()
        {
            if (Card != null) ResetCard(_shown);
        }

        void ResetCard(bool visible)
        {
            _spending = false;
            _pop = 1f;
            _hover = Vector2.zero;
            Group.alpha = 1f;
            Card.anchoredPosition = Vector2.zero;
            Card.localScale = Vector3.one;
            Card.localRotation = Quaternion.Euler(0f, 0f, BaseRoll);
            Card.gameObject.SetActive(visible);
        }

        void Update()
        {
            if (Card == null || _spending || !Card.gameObject.activeSelf) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            _pop = Mathf.MoveTowards(_pop, 1f, dt / PopTime);
            float k = _pop - 1f;
            float pop = Mathf.Max(0f, 1f + 2.70158f * k * k * k + 1.70158f * k * k);

            var target = Vector2.zero;
            var rt = (RectTransform)transform;
            var mouse = Mouse.current;
            if (mouse != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, mouse.position.ReadValue(), null, out var local) && rt.rect.Contains(local))
                target = new Vector2(Mathf.Clamp(local.x / (rt.rect.width * 0.5f), -1f, 1f), Mathf.Clamp(local.y / (rt.rect.height * 0.5f), -1f, 1f));
            _hover = Vector2.Lerp(_hover, target, 1f - Mathf.Exp(-12f * dt));
            float lift = Mathf.Clamp01(_hover.magnitude);

            float tiltX = 10f * Mathf.Sin(_t * 1.7f) - _hover.y * 22f;
            float tiltY = 12f * Mathf.Cos(_t * 1.2f) + _hover.x * 22f;
            float roll = BaseRoll + 2.5f * Mathf.Sin(_t * 0.9f);
            Card.localRotation = Quaternion.Euler(tiltX, tiltY, roll);
            Card.localScale = Vector3.one * (pop * (1f + 0.03f * Mathf.Sin(_t * 2.4f) + 0.1f * lift));

            if (Glint != null)
            {
                float phase = (_t % GlintPeriod) / GlintSweep;
                float width = rt.rect.width;
                Glint.anchoredPosition = new Vector2(phase < 1f ? Mathf.Lerp(-width, width, phase) : width * 2f, 0f);
            }
        }

        IEnumerator Spend()
        {
            _spending = true;
            var from = Card.localRotation;
            for (float t = 0f; t < SpendTime; t += Time.unscaledDeltaTime)
            {
                float k = t / SpendTime;
                float scale = k < 0.2f ? 1f + 0.9f * k : Mathf.Lerp(1.18f, 0f, (k - 0.2f) / 0.8f);
                Card.anchoredPosition = new Vector2(0f, SpendRise * k * k);
                Card.localScale = Vector3.one * scale;
                Card.localRotation = from * Quaternion.Euler(0f, 0f, 45f * k);
                Group.alpha = 1f - k * k;
                yield return null;
            }
            ResetCard(false);
        }
    }
}
